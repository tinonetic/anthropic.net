namespace Anthropic.Net;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The Anthropic API client.
/// </summary>
public class AnthropicApiClient : IAnthropicApiClient, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// The injected HttpClientFactory objects object to be used to create the HttpClient.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string _apiBaseUrl;
    private readonly ServiceProvider? _internalServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class with internal HttpClient management.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    public AnthropicApiClient(string apiKey, string apiBaseUrl = "https://api.anthropic.com")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "Please provide the Anthropic API key.");
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentNullException(nameof(apiBaseUrl), "Please assign the 'apiBaseUrl'.");
        }

        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;

        // Bootstrap internal ServiceProvider to get IHttpClientFactory
        var services = new ServiceCollection();
        services.AddHttpClient();
        _internalServiceProvider = services.BuildServiceProvider();
        _httpClientFactory = _internalServiceProvider.GetRequiredService<IHttpClientFactory>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class with an injected IHttpClientFactory.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="httpClientFactory">The injected HttpClientFactory.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    public AnthropicApiClient(string apiKey, IHttpClientFactory httpClientFactory, string apiBaseUrl = "https://api.anthropic.com")
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "Please provide the Anthropic API key.");
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentNullException(nameof(apiBaseUrl), "Please assign the 'apiBaseUrl'.");
        }

        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;
    }

    /// <summary>
    /// Sends a prompt to the Anthropic API for completion.
    /// </summary>
    /// <param name="request">The <see cref="CompletionRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="CompletionResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    public async Task<CompletionResponse> CompletionAsync(CompletionRequest request)
    {
        // Validate
        ValidateRequest(request);

        // Create an HTTP request message with the API endpoint URL and HTTP method
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v1/complete");

        // Set the Accept and X-API-Key headers for the API request
        httpRequestMessage.Headers.Accept.Clear();
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Headers.Add("X-API-Key", _apiKey);

        // Serialize the CompleteRequest object to JSON and set it as the request body
        var jsonPayload = JsonSerializer.Serialize(request);
        httpRequestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Send the API request and get the response
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(true);

        // Check if the API request was successful
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            throw new AnthropicApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        // Deserialize the API response JSON into a CompleteResponse object
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        CompletionResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<CompletionResponse>(json);
        }
        catch (JsonException ex)
        {
            // Throw an exception if deserialization fails
            throw new AnthropicApiException("Error deserializing API response.", ex);
        }

        // Throw an exception if the API response is null
        if (result == null)
        {
            throw new AnthropicApiException("API response was null.");
        }

        // Return the CompleteResponse object
        return result;
    }

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
    /// Sends a message to the Anthropic API using the Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessageRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="MessageResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    public async Task<MessageResponse> MessageAsync(MessageRequest request)
    {
        // Validate
        ValidateMessageRequest(request);

        // Create an HTTP request message with the API endpoint URL and HTTP method
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v1/messages");

        // Set the Accept and API key headers for the API request
        httpRequestMessage.Headers.Accept.Clear();
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Headers.Add("anthropic-version", "2023-06-01");
        httpRequestMessage.Headers.Add("x-api-key", _apiKey);

        // Serialize the MessageRequest object to JSON and set it as the request body
        var jsonPayload = JsonSerializer.Serialize(request);
        httpRequestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Send the API request and get the response
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(true);

        // Check if the API request was successful
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            throw new AnthropicApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        // Deserialize the API response JSON into a MessageResponse object
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        MessageResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<MessageResponse>(json);
        }
        catch (JsonException ex)
        {
            // Throw an exception if deserialization fails
            throw new AnthropicApiException("Error deserializing API response.", ex);
        }

        // Throw an exception if the API response is null
        if (result == null)
        {
            throw new AnthropicApiException("API response was null.");
        }

        // Return the MessageResponse object
        return result;
    }

    /// <summary>
    /// Validates that the specified request parameters are valid according to the MessageRequest specification.
    /// </summary>
    /// <param name="request">The MessageRequest object to validate.</param>
    /// <exception cref="ArgumentException">Thrown if any of the required parameters are missing or invalid.</exception>
    private static void ValidateMessageRequest(MessageRequest request)
    {
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
    /// <inheritdoc/>
    public async IAsyncEnumerable<MessageStreamEvent> StreamMessageAsync(MessageRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateMessageRequest(request);

        request.Stream = true;

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v1/messages");
        httpRequestMessage.Headers.Accept.Clear();
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequestMessage.Headers.Add("anthropic-version", "2023-06-01");
        httpRequestMessage.Headers.Add("x-api-key", _apiKey);

        var jsonPayload = JsonSerializer.Serialize(request);
        httpRequestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new AnthropicApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line["event:".Length..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var data = line["data:".Length..].Trim();
                if (currentEvent != null)
                {
                    MessageStreamEvent? streamEvent = null;
                    try
                    {
                        streamEvent = currentEvent switch
                        {
                            "message_start" => JsonSerializer.Deserialize<MessageStartEvent>(data),
                            "content_block_start" => JsonSerializer.Deserialize<ContentBlockStartEvent>(data),
                            "content_block_delta" => JsonSerializer.Deserialize<ContentBlockDeltaEvent>(data),
                            "content_block_stop" => JsonSerializer.Deserialize<ContentBlockStopEvent>(data),
                            "message_delta" => JsonSerializer.Deserialize<MessageDeltaEvent>(data),
                            "message_stop" => JsonSerializer.Deserialize<MessageStopEvent>(data),
                            "ping" => JsonSerializer.Deserialize<PingEvent>(data),
                            "error" => JsonSerializer.Deserialize<ErrorEvent>(data),
                            _ => null,
                        };
                    }
                    catch (JsonException)
                    {
                        // Ignore deserialization errors for now or log them
                    }

                    if (streamEvent != null)
                    {
                        streamEvent.Type = currentEvent;
                        yield return streamEvent;
                    }
                }
            }
        }
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
}
