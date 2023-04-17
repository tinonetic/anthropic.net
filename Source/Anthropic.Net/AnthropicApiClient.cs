namespace Anthropic.Net;

using System.Net.Http;
using System.Net.Http.Headers;

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
    /// Base method for sending a prompt to Claude for completion.
    /// </summary>
    /// <param name="parameters">A dictionary of parameters to include in the request, according to the reference.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    /// <remarks>
    /// The <paramref name="parameters"/> parameter should be a relative URL that will be combined with the base API URL.
    /// For more information, see the <a href="https://console.anthropic.com/docs/api/reference">Anthropic API Reference</a>.
    /// </remarks>
    public async Task<JsonElement> CompletionAsync(Dictionary<string, object> parameters)
    {
        ValidateRequest(parameters);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v1/complete");
        httpRequestMessage.Headers.Accept.Clear();
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Headers.Add("X-API-Key", _apiKey);
        httpRequestMessage.Content = new FormUrlEncodedContent(parameters.Select(x => new KeyValuePair<string, string>(x.Key, (string)x.Value)));
        httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(true);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            throw new AnthropicApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    private static void ValidateRequest(Dictionary<string, object> parameters)
    {
        if (!parameters.ContainsKey("prompt"))
        {
            throw new ValidationException("The prompt parameter is missing.");
        }
    }
}
