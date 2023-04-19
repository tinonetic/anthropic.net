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
    /// The injected HttpClientFactory objects object to be used to create the HttpClient.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string _apiBaseUrl;

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
    public AnthropicApiClient(string apiKey, IHttpClientFactory httpClientFactory, string apiBaseUrl = "https://api.anthropic.com")
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory), "Please instantiate you HttpClient. Recommended to use HttpClientFactory. See example projects.");
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new ArgumentNullException(nameof(apiBaseUrl), "Please assign the 'apiBaseUrl");
        }

        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicApiClient"/> class.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="httpClientFactory">
    /// The HttpClientFactory object that creates the HttpClient used to make requests.
    /// HttpClient should be initialized with the API key and Base URL. See example.
    /// </param>
    /// <remarks>
    /// The <paramref name="httpClientFactory"/> parameter is recommended to be injected via .NET dependency injection.
    /// Avoid using use "new HttpClient()".
    /// For more information, look at the Examples from the <a href="https://github.com/tinonetic/anthropic.net">Anthropic.Net repository</a> and for details see <a href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">Use IHttpClientFactory to implement resilient HTTP requests</a>.
    /// </remarks>
    public AnthropicApiClient(string apiKey, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory), "Please instantiate you HttpClient. Recommended to use HttpClientFactory. See example projects.");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "Please provide the Anthropic API key. See the API Reference for details: https://console.anthropic.com/docs/api/reference");
        }

        _apiKey = apiKey;
        _apiBaseUrl = "https://api.anthropic.com";
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
}
