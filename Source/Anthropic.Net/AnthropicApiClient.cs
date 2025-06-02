namespace Anthropic.Net;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>
/// The Anthropic API client.
/// </summary>
public class AnthropicApiClient : IAnthropicApiClient
{
    /// <summary>
    /// Internally managed HttpClientFactory.
    /// </summary>
    private static readonly IHttpClientFactory _internalHttpClientFactory;
    private readonly HttpClient? _testHttpClient; // For testing purposes
    private const string AnthropicVersion = "2023-06-01"; // API version header
    private readonly string? _apiKey;
    private readonly string _apiBaseUrl;

    /// <summary>
    /// Static constructor to initialize the internal HttpClientFactory.
    /// </summary>
    static AnthropicApiClient()
    {
        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        serviceCollection.AddHttpClient();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _internalHttpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    /// <param name="httpClientFactory">
    /// The HttpClientFactory object that creates the HttpClient used to make requests.
    /// HttpClient should be initialized with the API key and Base URL. See example.
    /// </param>
    /// <remarks>
    /// The <paramref name="httpClientFactory"/> parameter is recommended to be injected via .NET dependency injection.
    /// Avoid using use "new HttpClient()".
    /// For more information, look at the Examples from the <a href="https://github.com/tinonetic/anthropic.net">Anthropic.Net repository</a> and for details see <a href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">Use IHttpClientFactory to implement resilient HTTP requests</a>.
    /// </remarks>
    public AnthropicApiClient(string apiKey, string apiBaseUrl = "https://api.anthropic.com")
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentNullException(nameof(apiBaseUrl), "Please assign the 'apiBaseUrl");
        }

        if (string.IsNullOrWhiteSpace(apiKey)) 
        {
            throw new ArgumentNullException(nameof(apiKey), "Please provide the Anthropic API key.");
        }

        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;
        // _internalHttpClientFactory is initialized by the static constructor
        // _testHttpClient will be null for this public constructor
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <remarks>
    /// This constructor uses the default Anthropic API base URL.
    /// </remarks>
    public AnthropicApiClient(string apiKey) : this(apiKey, "https://api.anthropic.com")
    {
        // API key validation is handled by the constructor this delegates to.
    }

    /// <summary>
    /// Internal constructor for testing, allowing injection of HttpMessageHandler.
    /// </summary>
    internal AnthropicApiClient(string apiKey, HttpMessageHandler handler, string apiBaseUrl = "https://api.anthropic.com")
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentNullException(nameof(apiBaseUrl));
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey));
        }
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;
        _testHttpClient = new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
        // _internalHttpClientFactory is still initialized by the static constructor but won't be used if _testHttpClient is present.
    }

    /// <summary>
    /// Sends a prompt to the Anthropic API for completion.
    /// </summary>
    /// <param name="request">The <see cref="CompletionRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="CompletionResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    [Obsolete("This method uses the legacy Completions API. Prefer using CreateMessageAsync or StreamMessageAsync with the Messages API.")]
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
        var httpClient = _testHttpClient ?? _internalHttpClientFactory.CreateClient();
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
    /// Creates a message using the Anthropic Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessagesRequest"/> object representing the request parameters.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation. The result is a <see cref="MessagesResponse"/> object representing the response from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails or the API response cannot be deserialized.</exception>
    public async Task<MessagesResponse> CreateMessageAsync(MessagesRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Ensure stream is false for this non-streaming method
        request.Stream = false;

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v2/messages");

        httpRequestMessage.Headers.Accept.Clear();
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Headers.Add("X-API-Key", _apiKey);
        httpRequestMessage.Headers.Add("anthropic-version", AnthropicVersion);

        var jsonPayloadOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var jsonPayload = JsonSerializer.Serialize(request, jsonPayloadOptions);
        httpRequestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var httpClient = _testHttpClient ?? _internalHttpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false); // Use false for library code

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new AnthropicApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        MessagesResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<MessagesResponse>(json);
        }
        catch (JsonException ex)
        {
            throw new AnthropicApiException("Error deserializing API response.", ex);
        }

        if (result == null)
        {
            throw new AnthropicApiException("API response was null.");
        }

        return result;
    }

    /// <summary>
    /// Streams a message using the Anthropic Messages API.
    /// </summary>
    /// <param name="request">The <see cref="MessagesRequest"/> object representing the request parameters.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the streaming operation.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="MessagesStreamEvent"/> representing the stream of events from the API.</returns>
    /// <exception cref="AnthropicApiException">Thrown if the API request fails.</exception>
    public async IAsyncEnumerable<MessagesStreamEvent> StreamMessageAsync(
        MessagesRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Stream = true;

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v2/messages");

        httpRequestMessage.Headers.Accept.Clear();
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream")); // SSE expects this
        httpRequestMessage.Headers.Add("X-API-Key", _apiKey);
        httpRequestMessage.Headers.Add("anthropic-version", AnthropicVersion);
        // httpRequestMessage.Headers.ConnectionClose = false; // Keep connection alive for streaming

        var jsonPayloadOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var jsonPayload = JsonSerializer.Serialize(request, jsonPayloadOptions);
        httpRequestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        var httpClient = _testHttpClient ?? _internalHttpClientFactory.CreateClient();
        try
        {
            response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new AnthropicApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false); // For .NET 6+ ReadAsStreamAsync(cancellationToken)
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string currentEventType = null;
            StringBuilder currentEventData = new StringBuilder();

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(line)) // An empty line signifies the end of an event
                {
                    if (!string.IsNullOrWhiteSpace(currentEventType) && currentEventData.Length > 0)
                    {
                        var eventData = currentEventData.ToString();
                        MessagesStreamEvent streamEvent = null;
                        try
                        {
                            streamEvent = new MessagesStreamEvent { Type = currentEventType };

                            // Deserialize the data portion into the correct property
                            // based on the event type.
                            switch (currentEventType)
                            {
                                case MessageStreamEventTypes.MessageStart:
                                    streamEvent.Message = JsonSerializer.Deserialize<MessageStartDetails>(eventData);
                                    break;
                                case MessageStreamEventTypes.ContentBlockStart:
                                    // ContentBlockStart event data includes 'index' and 'content_block'
                                    // We need to parse these from the eventData
                                    using (JsonDocument doc = JsonDocument.Parse(eventData))
                                    {
                                        if (doc.RootElement.TryGetProperty("index", out JsonElement indexElement))
                                        {
                                            streamEvent.Index = indexElement.GetInt32();
                                        }
                                        if (doc.RootElement.TryGetProperty("content_block", out JsonElement contentBlockElement))
                                        {
                                            streamEvent.ContentBlock = JsonSerializer.Deserialize<ContentBlock>(contentBlockElement.GetRawText());
                                        }
                                    }
                                    break;
                                case MessageStreamEventTypes.ContentBlockDelta:
                                     using (JsonDocument doc = JsonDocument.Parse(eventData))
                                    {
                                        if (doc.RootElement.TryGetProperty("index", out JsonElement indexElement))
                                        {
                                            streamEvent.Index = indexElement.GetInt32();
                                        }
                                        if (doc.RootElement.TryGetProperty("delta", out JsonElement deltaElement))
                                        {
                                            streamEvent.Delta = JsonSerializer.Deserialize<DeltaDetails>(deltaElement.GetRawText());
                                        }
                                    }
                                    break;
                                case MessageStreamEventTypes.MessageDelta:
                                    // MessageDelta event data includes 'delta' (with stop_reason, etc.) and 'usage'
                                    using (JsonDocument doc = JsonDocument.Parse(eventData))
                                    {
                                        if (doc.RootElement.TryGetProperty("delta", out JsonElement deltaElement))
                                        {
                                            streamEvent.Delta = JsonSerializer.Deserialize<DeltaDetails>(deltaElement.GetRawText());
                                        }
                                        if (doc.RootElement.TryGetProperty("usage", out JsonElement usageElement))
                                        {
                                            streamEvent.Usage = JsonSerializer.Deserialize<UsageInfo>(usageElement.GetRawText());
                                        }
                                    }
                                    break;
                                case MessageStreamEventTypes.Error:
                                    // The 'data' for an error event is an error object, not the full StreamError structure
                                    var apiError = JsonSerializer.Deserialize<ApiError>(eventData);
                                    streamEvent.Error = new StreamError { Type = "error", ErrorDetails = apiError }; // Reconstruct StreamError
                                    break;
                                case MessageStreamEventTypes.Ping:
                                case MessageStreamEventTypes.MessageStop:
                                case MessageStreamEventTypes.ContentBlockStop:
                                    // These events might not have data or it's not complex.
                                    // The type itself is the main information.
                                    // If they have simple data, it might need to be handled or ignored.
                                    break;
                                default:
                                    // Log or handle unknown event types, or ignore
                                    // For now, we yield it if it was parsable by the initial attempt,
                                    // or it will be null and not yielded.
                                    // This case should ideally not be hit if all event types are known.
                                    break;
                            }
                        }
                        catch (JsonException ex)
                        {
                            throw new AnthropicApiException($"Error deserializing stream event data for event type {currentEventType}: {eventData}", ex);
                        }
                        
                        if (streamEvent != null)
                        {
                            yield return streamEvent;
                        }
                    }
                    currentEventType = null;
                    currentEventData.Clear();
                }
                else if (line.StartsWith("event:"))
                {
                    currentEventType = line.Substring("event:".Length).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    currentEventData.Append(line.Substring("data:".Length).Trim());
                }
                // Other lines (e.g., comments starting with ':') are ignored by this logic.
            }
        }
        catch (OperationCanceledException)
        {
            // Catch cancellation token exceptions and let them propagate to stop the enumerable
            throw;
        }
        catch (Exception ex)
        {
            // General exception handling
            throw new AnthropicApiException("Error processing message stream.", ex);
        }
        finally
        {
            response?.Dispose();
        }
    }
}
