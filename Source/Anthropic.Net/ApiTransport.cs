namespace Anthropic.Net;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

/// <summary>
/// Shared HTTP layer: authentication headers, beta headers, retries with backoff, error mapping and JSON helpers.
/// Used by <see cref="AnthropicApiClient"/> and the Managed Agents, Skills and Admin clients.
/// </summary>
internal sealed class ApiTransport
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string? _authToken;
    private readonly string _apiVersion;
    private readonly int _maxRetries;
    private readonly TimeSpan? _timeout;
    private readonly IList<string> _defaultBetas;
    private readonly bool _sendOAuthBeta;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpRequestMessage>>? _interceptor;

    internal ApiTransport(AnthropicClientOptions options, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = string.IsNullOrWhiteSpace(options.ApiKey) ? null : options.ApiKey;

        // The API rejects requests carrying both credentials, so an API key wins.
        _authToken = _apiKey is null ? options.AuthToken : null;
        BaseUrl = options.BaseUrl.TrimEnd('/');
        _apiVersion = options.ApiVersion;
        _maxRetries = Math.Max(0, options.MaxRetries);
        _timeout = options.Timeout;
        _defaultBetas = options.Betas;
        _sendOAuthBeta = options.SendOAuthBeta;
        _interceptor = options.RequestInterceptor;
        AdminBetas = options.AdminBetas;
    }

    internal string BaseUrl { get; }

    internal IList<string> AdminBetas { get; }

    internal static T Deserialize<T>(string json)
    {
        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(json, AnthropicJson.Options);
        }
        catch (JsonException ex)
        {
            throw new AnthropicApiException("Error deserializing API response.", ex);
        }

        return result ?? throw new AnthropicApiException("API response was null.");
    }

    internal static string Query(params (string Key, object? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.Value is not null && !(p.Value is string s && s.Length == 0))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(Convert.ToString(p.Value, System.Globalization.CultureInfo.InvariantCulture)!.ToLowerInvariantIfBool(p.Value))}")
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    internal HttpRequestMessage JsonRequest(HttpMethod method, string path, string? json, string accept = "application/json")
    {
        var message = new HttpRequestMessage(method, $"{BaseUrl}{path}");
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        if (json is not null)
        {
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return message;
    }

    internal async Task<T> PostAsync<T>(string path, string json, IEnumerable<string>? betas, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(() => JsonRequest(HttpMethod.Post, path, json), betas, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
    }

    internal async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(() => JsonRequest(HttpMethod.Get, path, null), null, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Sends a JSON request and deserializes the JSON response.</summary>
    internal async Task<T> RequestAsync<T>(HttpMethod method, string path, object? body, IEnumerable<string>? betas, CancellationToken cancellationToken)
    {
        var json = body is null ? null : (body as string) ?? JsonSerializer.Serialize(body, body.GetType(), AnthropicJson.Options);
        using var response = await SendAsync(() => JsonRequest(method, path, json), betas, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(text);
    }

    /// <summary>Sends a JSON request and ignores the response body (DELETE / 204).</summary>
    internal async Task RequestAsync(HttpMethod method, string path, object? body, IEnumerable<string>? betas, CancellationToken cancellationToken)
    {
        var json = body is null ? null : (body as string) ?? JsonSerializer.Serialize(body, body.GetType(), AnthropicJson.Options);
        using var response = await SendAsync(() => JsonRequest(method, path, json), betas, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Opens a Server-Sent Events stream and yields each <c>data:</c> payload parsed as <typeparamref name="T"/>; malformed or unknown payloads are skipped.</summary>
    internal async IAsyncEnumerable<T> StreamSseAsync<T>(HttpMethod method, string path, object? body, IEnumerable<string>? betas, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var json = body is null ? null : JsonSerializer.Serialize(body, body.GetType(), AnthropicJson.Options);
        using var response = await SendAsync(() => JsonRequest(method, path, json, "text/event-stream"), betas, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            T? item = default;
            try
            {
                item = JsonSerializer.Deserialize<T>(line["data:".Length..].Trim(), AnthropicJson.Options);
            }
            catch (JsonException)
            {
                // Skip payloads this SDK cannot parse so new event shapes never break consumers.
            }

            if (item is not null)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Sends a request, retrying 408/409/429/5xx (including 529 overloaded) and network failures with exponential
    /// backoff (honouring retry-after). Throws <see cref="AnthropicApiException"/> for non-success responses.
    /// </summary>
    internal async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> build, IEnumerable<string>? betas, HttpCompletionOption completion, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        if (_timeout is not null)
        {
            try
            {
                httpClient.Timeout = _timeout.Value;
            }
            catch (InvalidOperationException)
            {
                // Shared client already started; keep its configured timeout.
            }
        }

        for (var attempt = 0; ; attempt++)
        {
            using var original = build();
            ApplyHeaders(original, betas);
            var request = _interceptor is null ? original : await _interceptor(original, cancellationToken).ConfigureAwait(false);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, completion, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                if (!ReferenceEquals(request, original))
                {
                    request.Dispose();
                }

                if (attempt >= _maxRetries)
                {
                    throw new AnthropicApiException("Connection error: " + ex.Message, ex);
                }

                await Task.Delay(Backoff(attempt, null), cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (!ReferenceEquals(request, original))
            {
                request.Dispose();
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var status = (int)response.StatusCode;
            var retryable = status is 408 or 409 or 429 or >= 500;
            if (retryable && attempt < _maxRetries)
            {
                var delay = Backoff(attempt, response.Headers.RetryAfter?.Delta);
                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                throw await ToExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
            }
        }
    }

    private static TimeSpan Backoff(int attempt, TimeSpan? retryAfter)
    {
        if (retryAfter is { } ra && ra > TimeSpan.Zero && ra <= TimeSpan.FromSeconds(60))
        {
            return ra;
        }

        return TimeSpan.FromMilliseconds(Math.Min(8000, 500 * Math.Pow(2, attempt)));
    }

    private static async Task<AnthropicApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string? errorType = null;
        string? requestId = response.Headers.TryGetValues("request-id", out var ids) ? ids.FirstOrDefault() : null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("type", out var t))
            {
                errorType = t.GetString();
            }

            if (doc.RootElement.TryGetProperty("request_id", out var rid) && rid.ValueKind == JsonValueKind.String)
            {
                requestId ??= rid.GetString();
            }
        }
        catch (JsonException)
        {
            // Non-JSON error body (e.g. from a proxy); keep the raw text in the message.
        }

        return new AnthropicApiException($"Request failed with status code {response.StatusCode}: {body}", (int)response.StatusCode, errorType, requestId);
    }

    private void ApplyHeaders(HttpRequestMessage message, IEnumerable<string>? betas)
    {
        message.Headers.Add("anthropic-version", _apiVersion);
        if (_apiKey is not null)
        {
            message.Headers.Add("x-api-key", _apiKey);
        }
        else if (_authToken is not null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        }

        var allBetas = _defaultBetas.Concat(betas ?? []).Distinct().ToList();
        if (_sendOAuthBeta && _authToken is not null && _apiKey is null && !allBetas.Contains("oauth-2025-04-20"))
        {
            allBetas.Add("oauth-2025-04-20");
        }

        if (allBetas.Count > 0)
        {
            message.Headers.Add("anthropic-beta", string.Join(',', allBetas));
        }
    }
}

internal static class QueryExtensions
{
    internal static string ToLowerInvariantIfBool(this string text, object? original) => original is bool ? text.ToLowerInvariant() : text;
}
