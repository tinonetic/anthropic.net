namespace Anthropic.Net;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.Net.Admin;
using Anthropic.Net.ManagedAgents;
using Anthropic.Net.Models.Batches;
using Anthropic.Net.Models.Files;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;
using Anthropic.Net.Models.Models;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The Anthropic API client.
/// </summary>
public class AnthropicApiClient : IAnthropicApiClient, IDisposable
{
    private static readonly string[] CountTokensKeys =
    [
        "model", "messages", "system", "tools", "tool_choice", "thinking", "output_config", "mcp_servers", "cache_control", "context_management",
    ];

    private readonly ApiTransport _transport;
    private readonly ServiceProvider? _internalServiceProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class with internal HttpClient management.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    public AnthropicApiClient(string apiKey, string apiBaseUrl = "https://api.anthropic.com")
        : this(FromKey(apiKey, apiBaseUrl))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class with an injected IHttpClientFactory.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="httpClientFactory">The injected HttpClientFactory.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    public AnthropicApiClient(string apiKey, IHttpClientFactory httpClientFactory, string apiBaseUrl = "https://api.anthropic.com")
        : this(FromKey(apiKey, apiBaseUrl), httpClientFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class from options (internal HttpClient management).
    /// </summary>
    /// <param name="options">The client options.</param>
    public AnthropicApiClient(AnthropicClientOptions options)
        : this(options, CreateInternalFactory(out var provider))
    {
        _internalServiceProvider = provider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class from options and an injected IHttpClientFactory.
    /// </summary>
    /// <param name="options">The client options.</param>
    /// <param name="httpClientFactory">The injected HttpClientFactory.</param>
    public AnthropicApiClient(AnthropicClientOptions options, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        if (string.IsNullOrWhiteSpace(options.ApiKey) && string.IsNullOrWhiteSpace(options.AuthToken) && options.RequestInterceptor is null)
        {
            throw new ArgumentNullException(nameof(options), "Please provide the Anthropic API key (or an auth token).");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentNullException(nameof(options), "Please assign the 'BaseUrl'.");
        }

        _transport = new ApiTransport(options, httpClientFactory);
        ManagedAgents = new ManagedAgentsApi(_transport);
        Skills = new SkillsApi(_transport);
        Admin = new AdminApi(_transport);
    }

    /// <inheritdoc/>
    public ManagedAgentsApi ManagedAgents { get; }

    /// <inheritdoc/>
    public SkillsApi Skills { get; }

    /// <inheritdoc/>
    public AdminApi Admin { get; }

    /// <summary>
    /// Sends a prompt to the legacy Text Completions API.
    /// </summary>
    /// <param name="request">The <see cref="CompletionRequest"/> object representing the request parameters.</param>
    /// <returns>The completion response.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    [Obsolete("The Text Completions API is legacy. Use MessageAsync.")]
    public async Task<CompletionResponse> CompletionAsync(CompletionRequest request)
    {
        ValidateRequest(request);
        var payload = JsonSerializer.Serialize(request, AnthropicJson.Options);
        return await PostAsync<CompletionResponse>("/v1/complete", payload, null, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<MessageResponse> MessageAsync(MessageRequest request, CancellationToken cancellationToken = default)
    {
        ValidateMessageRequest(request);
        request.Stream = false;
        return await PostAsync<MessageResponse>("/v1/messages", JsonSerializer.Serialize(request, AnthropicJson.Options), request.Betas, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(MessageRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateMessageRequest(request);
        request.Stream = true;
        var payload = JsonSerializer.Serialize(request, AnthropicJson.Options);

        using var response = await SendAsync(
            () => JsonRequest(HttpMethod.Post, "/v1/messages", payload, "text/event-stream"),
            request.Betas,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line["event:".Length..].Trim();
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            MessageStreamEvent? streamEvent = null;
            try
            {
                streamEvent = JsonSerializer.Deserialize<MessageStreamEvent>(line["data:".Length..].Trim(), AnthropicJson.Options);
            }
            catch (JsonException)
            {
                // Unknown event types are skipped so new API events never break existing consumers.
            }

            if (streamEvent != null)
            {
                streamEvent.Type = currentEvent ?? EventName(streamEvent);
                yield return streamEvent;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<TokenCount> CountTokensAsync(MessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Stream = false;
        var full = JsonSerializer.SerializeToNode(request, AnthropicJson.Options)!.AsObject();
        var body = new JsonObject();
        foreach (var key in CountTokensKeys)
        {
            if (full.TryGetPropertyValue(key, out var value))
            {
                body[key] = value?.DeepClone();
            }
        }

        return await PostAsync<TokenCount>("/v1/messages/count_tokens", body.ToJsonString(), request.Betas, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<Page<ModelInfo>> ListModelsAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken cancellationToken = default)
        => GetAsync<Page<ModelInfo>>("/v1/models" + Query(limit, afterId, beforeId), cancellationToken);

    /// <inheritdoc/>
    public Task<ModelInfo> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
        => GetAsync<ModelInfo>($"/v1/models/{Uri.EscapeDataString(modelId)}", cancellationToken);

    /// <inheritdoc/>
    public Task<MessageBatch> CreateBatchAsync(IEnumerable<BatchRequestItem> requests, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var items = requests.ToList();
        if (items.Count == 0)
        {
            throw new ArgumentException("A batch must contain at least one request.", nameof(requests));
        }

        foreach (var item in items)
        {
            ValidateMessageRequest(item.Params);
            item.Params.Stream = false;
        }

        var payload = JsonSerializer.Serialize(new { requests = items }, AnthropicJson.Options);
        return PostAsync<MessageBatch>("/v1/messages/batches", payload, null, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<MessageBatch> GetBatchAsync(string batchId, CancellationToken cancellationToken = default)
        => GetAsync<MessageBatch>($"/v1/messages/batches/{Uri.EscapeDataString(batchId)}", cancellationToken);

    /// <inheritdoc/>
    public Task<Page<MessageBatch>> ListBatchesAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken cancellationToken = default)
        => GetAsync<Page<MessageBatch>>("/v1/messages/batches" + Query(limit, afterId, beforeId), cancellationToken);

    /// <inheritdoc/>
    public Task<MessageBatch> CancelBatchAsync(string batchId, CancellationToken cancellationToken = default)
        => PostAsync<MessageBatch>($"/v1/messages/batches/{Uri.EscapeDataString(batchId)}/cancel", "{}", null, cancellationToken);

    /// <inheritdoc/>
    public async Task DeleteBatchAsync(string batchId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => JsonRequest(HttpMethod.Delete, $"/v1/messages/batches/{Uri.EscapeDataString(batchId)}", null),
            null,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<BatchResultItem> GetBatchResultsAsync(string batchId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => JsonRequest(HttpMethod.Get, $"/v1/messages/batches/{Uri.EscapeDataString(batchId)}/results", null, "application/binary"),
            null,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var item = Deserialize<BatchResultItem>(line);
            yield return item;
        }
    }

    /// <inheritdoc/>
    public async Task<FileMetadata> UploadFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Buffer once so retries can replay the body.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var bytes = buffer.ToArray();

        using var response = await SendAsync(
            () =>
            {
                var file = new ByteArrayContent(bytes);
                file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                var form = new MultipartFormDataContent { { file, "file", fileName } };
                var message = new HttpRequestMessage(HttpMethod.Post, $"{_transport.BaseUrl}/v1/files") { Content = form };
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return message;
            },
            null,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);

        return Deserialize<FileMetadata>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public Task<Page<FileMetadata>> ListFilesAsync(int? limit = null, string? afterId = null, string? beforeId = null, CancellationToken cancellationToken = default)
        => GetAsync<Page<FileMetadata>>("/v1/files" + Query(limit, afterId, beforeId), cancellationToken);

    /// <inheritdoc/>
    public Task<FileMetadata> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
        => GetAsync<FileMetadata>($"/v1/files/{Uri.EscapeDataString(fileId)}", cancellationToken);

    /// <inheritdoc/>
    public async Task<byte[]> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => JsonRequest(HttpMethod.Get, $"/v1/files/{Uri.EscapeDataString(fileId)}/content", null, "application/binary"),
            null,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => JsonRequest(HttpMethod.Delete, $"/v1/files/{Uri.EscapeDataString(fileId)}", null),
            null,
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the allocated resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the AnthropicApiClient and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _internalServiceProvider?.Dispose();
            }

            _disposed = true;
        }
    }

    private static AnthropicClientOptions FromKey(string apiKey, string apiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "Please provide the Anthropic API key.");
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentNullException(nameof(apiBaseUrl), "Please assign the 'apiBaseUrl'.");
        }

        return new AnthropicClientOptions { ApiKey = apiKey, AuthToken = null, BaseUrl = apiBaseUrl };
    }

    private static IHttpClientFactory CreateInternalFactory(out ServiceProvider provider)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IHttpClientFactory>();
    }

    private static string EventName(MessageStreamEvent e) => e switch
    {
        MessageStartEvent => "message_start",
        ContentBlockStartEvent => "content_block_start",
        ContentBlockDeltaEvent => "content_block_delta",
        ContentBlockStopEvent => "content_block_stop",
        MessageDeltaEvent => "message_delta",
        MessageStopEvent => "message_stop",
        PingEvent => "ping",
        ErrorEvent => "error",
        _ => string.Empty,
    };

    private static string Query(int? limit, string? afterId, string? beforeId)
    {
        var parts = new List<string>();
        if (limit is not null)
        {
            parts.Add($"limit={limit}");
        }

        if (!string.IsNullOrEmpty(afterId))
        {
            parts.Add($"after_id={Uri.EscapeDataString(afterId)}");
        }

        if (!string.IsNullOrEmpty(beforeId))
        {
            parts.Add($"before_id={Uri.EscapeDataString(beforeId)}");
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }

    private static T Deserialize<T>(string json) => ApiTransport.Deserialize<T>(json);

    /// <summary>
    /// Validates that the specified request parameters are valid according to the CompleteRequest specification.
    /// </summary>
    /// <param name="request">The CompleteRequest object to validate.</param>
    /// <exception cref="ArgumentException">Thrown if any of the required parameters are missing or invalid.</exception>
    private static void ValidateRequest(CompletionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("The 'request.Prompt' parameter is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("The 'request.Model' parameter is required.", nameof(request));
        }

        if (request.MaxTokensToSample <= 0)
        {
            throw new ArgumentException("The 'request.MaxTokensToSample' parameter must be greater than zero.", nameof(request));
        }

        if (request.StopSequences == null || request.StopSequences.Count == 0)
        {
            throw new ArgumentException("The 'request.StopSequences' parameter must contain at least one stop sequence.", nameof(request));
        }

        if (request.Temperature is < 0 or > 1)
        {
            throw new ArgumentException("The 'request.Temperature' parameter must be between 0 and 1, inclusive.", nameof(request));
        }

        if (request.TopK < -1)
        {
            throw new ArgumentException("The 'request.TopK' parameter must be greater than or equal to -1.", nameof(request));
        }

        if (request.TopP is < -1 or > 1)
        {
            throw new ArgumentException("The 'request.TopP' parameter must be between -1 and 1, inclusive.", nameof(request));
        }
    }

    /// <summary>
    /// Validates that the specified request parameters are valid according to the MessageRequest specification.
    /// </summary>
    /// <param name="request">The MessageRequest object to validate.</param>
    /// <exception cref="ArgumentException">Thrown if any of the required parameters are missing or invalid.</exception>
    private static void ValidateMessageRequest(MessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Model))
        {
            throw new ArgumentException("The 'request.Model' parameter is required.", nameof(request));
        }

        if (request.Messages == null || request.Messages.Count == 0)
        {
            throw new ArgumentException("The 'request.Messages' parameter must contain at least one message.", nameof(request));
        }

        if (request.MaxTokens <= 0)
        {
            throw new ArgumentException("The 'request.MaxTokens' parameter must be greater than zero.", nameof(request));
        }
    }

    private HttpRequestMessage JsonRequest(HttpMethod method, string path, string? json, string accept = "application/json")
        => _transport.JsonRequest(method, path, json, accept);

    private Task<T> PostAsync<T>(string path, string json, IEnumerable<string>? betas, CancellationToken cancellationToken)
        => _transport.PostAsync<T>(path, json, betas, cancellationToken);

    private Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
        => _transport.GetAsync<T>(path, cancellationToken);

    private Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> build, IEnumerable<string>? betas, HttpCompletionOption completion, CancellationToken cancellationToken)
        => _transport.SendAsync(build, betas, completion, cancellationToken);
}
