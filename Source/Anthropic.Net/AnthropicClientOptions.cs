namespace Anthropic.Net;

/// <summary>Configuration for <see cref="AnthropicApiClient"/>.</summary>
public class AnthropicClientOptions
{
    /// <summary>Gets or sets the API key (x-api-key). Defaults to env ANTHROPIC_API_KEY.</summary>
    public string? ApiKey { get; set; } = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    /// <summary>Gets or sets an OAuth/bearer token (Authorization: Bearer). Defaults to env ANTHROPIC_AUTH_TOKEN.</summary>
    public string? AuthToken { get; set; } = Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");

    /// <summary>Gets or sets the base URL. Defaults to env ANTHROPIC_BASE_URL or https://api.anthropic.com.</summary>
    public string BaseUrl { get; set; } = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "https://api.anthropic.com";

    /// <summary>Gets or sets the anthropic-version header.</summary>
    public string ApiVersion { get; set; } = "2023-06-01";

    /// <summary>Gets or sets how many times 408/409/429/5xx (incl. 529) responses and network errors are retried.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Gets or sets an optional request timeout applied to the HttpClient.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Gets or sets default beta feature ids sent in anthropic-beta on every request.</summary>
    public IList<string> Betas { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the oauth beta header is sent with bearer-token auth (disable for cloud platforms).</summary>
    public bool SendOAuthBeta { get; set; } = true;

    /// <summary>Gets or sets beta ids sent on Admin API (/v1/organizations) calls.</summary>
    public IList<string> AdminBetas { get; set; } = [];

    /// <summary>
    /// Gets or sets a hook run on every outgoing request after headers are applied. It may return the same message
    /// (e.g. after adding a signature) or a replacement (e.g. a rewritten URL/body). Used by the platform clients.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, Task<HttpRequestMessage>>? RequestInterceptor { get; set; }
}
