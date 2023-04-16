namespace Anthropic.Net;

using System.Net.Http.Headers;

using System.Text.Json;

/// <summary>
/// The Anthropic API client.
/// </summary>
public class ApiClient : IApiClient
{
    /// <summary>
    /// The injected HttpClient object to be used to make requests.
    /// </summary>
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _apiBaseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HttpClient object used to make requests. HttpClient should be initialized with the API key and Base URL. See example.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    /// <remarks>
    /// The <paramref name="httpClient"/> parameter is recommended to be injected via .NET dependency injection.
    /// Avoid using use "new HttpClient()".
    /// For more information, look at the Examples from the <a href="https://github.com/tinonetic/anthropic.net">Anthropic.Net repository</a> and for details see <a href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">Use IHttpClientFactory to implement resilient HTTP requests</a>.
    /// </remarks>
    public ApiClient(HttpClient httpClient, string apiBaseUrl = "https://api.anthropic.com")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "Please instantiate you HttpClient. Recommended to use HttpClientFactory. See example projects.");
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("Please assiggn the 'HttpClient.BaseAddress'");
        }

        _apiBaseUrl = apiBaseUrl;
        InitializeHttpClient();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <param name="httpClient">The HttpClient object used to make requests.</param>
    /// <param name="apiBaseUrl">The Anthropic API Base URL.</param>
    /// <remarks>
    /// The <paramref name="httpClient"/> parameter is recommended to be injected via .NET dependency injection.
    /// Avoid using use "new HttpClient()".
    /// For more information, look at the Examples from the <a href="https://github.com/tinonetic/anthropic.net">Anthropic.Net repository</a> and for details see <a href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">Use IHttpClientFactory to implement resilient HTTP requests</a>.
    /// </remarks>
    public ApiClient(string apiKey, HttpClient httpClient, string apiBaseUrl = "https://api.anthropic.com")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "Please provide the Anthropic API key. See the API Reference for details: https://console.anthropic.com/docs/api/reference");
        }

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "Please instantiate you HttpClient. Recommended to use HttpClientFactory. See example projects.");
        _apiKey = apiKey;
        _apiBaseUrl = apiBaseUrl;
        InitializeHttpClient();
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
        var response = await SendRequestAsync("POST", "/v1/complete", parameters).ConfigureAwait(true);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    private void InitializeHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_apiBaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string method, string path, Dictionary<string, object> parameters)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (parameters != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(parameters));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        var response = await _httpClient.SendAsync(request).ConfigureAwait(true);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
            throw new ApiException($"Request failed with status code {response.StatusCode}: {errorContent}");
        }

        return response;
    }
}
