namespace Anthropic.Net;

using System.Net.Http.Headers;

using System.Text.Json;

/// <summary>
/// The Anthropic API client.
/// </summary>
public class ApiClient
{
    /// <summary>
    /// The injected HttpClient object to be used to make requests.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HttpClient object used to make requests.
    /// </param>
    /// <remarks>
    /// The <paramref name="httpClient"/>is recommended to be injected via .NET dependency injection.
    /// Avoid using use "new HttpClient()".
    /// For more information, look at the Examples from the <a href="https://github.com/tinonetic/anthropic.net">Anthropic.Net repository</a> and for details see <a href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests">Use IHttpClientFactory to implement resilient HTTP requests</a>.
    /// </remarks>
    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "Please instantiate you HttpClient. Recommended to use HttpClientFactory. See example projects.");
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
