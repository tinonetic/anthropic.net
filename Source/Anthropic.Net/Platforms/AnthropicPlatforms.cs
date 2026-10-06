namespace Anthropic.Net.Platforms;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Amazon Bedrock (Mantle, Messages-API endpoint). Partner-operated: model ids carry an <c>anthropic.</c> prefix
/// (use <see cref="ModelId"/>), and only a subset of features exists (no Batches, Files, Models, Managed Agents, server web tools).
/// Authenticate with a Bedrock API key or SigV4 credentials.
/// </summary>
public static class AnthropicBedrockMantle
{
    /// <summary>Gets the model id Bedrock expects, e.g. <c>anthropic.claude-opus-5-5</c>.</summary>
    /// <param name="firstPartyId">The first-party model id.</param>
    /// <returns>The Bedrock id.</returns>
    public static string ModelId(string firstPartyId) => firstPartyId.StartsWith("anthropic.", StringComparison.Ordinal) ? firstPartyId : "anthropic." + firstPartyId;

    /// <summary>Configures options for Bedrock Mantle.</summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="region">The AWS region (required; no default).</param>
    /// <param name="apiKey">A Bedrock API key (sent as a bearer token). When null, requests are SigV4 signed.</param>
    /// <param name="credentials">SigV4 credentials; defaults to the AWS_* environment variables.</param>
    /// <param name="signingService">The SigV4 service name.</param>
    public static void Configure(AnthropicClientOptions options, string region, string? apiKey = null, AwsCredentials? credentials = null, string signingService = "bedrock-mantle")
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        options.BaseUrl = $"https://bedrock-mantle.{region}.api.aws/anthropic";
        options.SendOAuthBeta = false;
        options.ApiKey = null;
        options.AuthToken = apiKey;
        options.RequestInterceptor = apiKey is not null
            ? null
            : async (request, ct) =>
            {
                await AwsSigV4.SignAsync(request, credentials ?? AwsCredentials.FromEnvironment(), region, signingService, null, ct).ConfigureAwait(false);
                return request;
            };
    }

    /// <summary>Creates a client for Bedrock Mantle.</summary>
    /// <param name="region">The AWS region.</param>
    /// <param name="apiKey">A Bedrock API key; when null requests are SigV4 signed.</param>
    /// <param name="credentials">SigV4 credentials; defaults to the AWS_* environment variables.</param>
    /// <returns>The client.</returns>
    public static AnthropicApiClient Create(string region, string? apiKey = null, AwsCredentials? credentials = null)
    {
        var options = new AnthropicClientOptions();
        Configure(options, region, apiKey, credentials);
        return new AnthropicApiClient(options);
    }
}

/// <summary>
/// Claude Platform on AWS: Anthropic-operated with full first-party API parity, billed and authorized through AWS.
/// Model ids are the bare first-party ids. Region and workspace id are required (no defaults).
/// </summary>
public static class AnthropicAws
{
    /// <summary>Configures options for Claude Platform on AWS (SigV4, service aws-external-anthropic).</summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="region">The AWS region; defaults to AWS_REGION.</param>
    /// <param name="workspaceId">The Claude workspace id; defaults to ANTHROPIC_AWS_WORKSPACE_ID.</param>
    /// <param name="credentials">SigV4 credentials; defaults to the AWS_* environment variables.</param>
    /// <param name="apiKey">A short-term API key (bearer) instead of SigV4.</param>
    /// <param name="workspaceHeader">The header that carries the workspace id (verify against the platform docs).</param>
    public static void Configure(AnthropicClientOptions options, string? region = null, string? workspaceId = null, AwsCredentials? credentials = null, string? apiKey = null, string workspaceHeader = "anthropic-workspace-id")
    {
        ArgumentNullException.ThrowIfNull(options);
        region ??= Environment.GetEnvironmentVariable("AWS_REGION");
        workspaceId ??= Environment.GetEnvironmentVariable("ANTHROPIC_AWS_WORKSPACE_ID");
        if (string.IsNullOrWhiteSpace(region))
        {
            throw new ArgumentException("An AWS region is required (argument or AWS_REGION).", nameof(region));
        }

        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            throw new ArgumentException("A workspace id is required (argument or ANTHROPIC_AWS_WORKSPACE_ID).", nameof(workspaceId));
        }

        options.BaseUrl = $"https://aws-external-anthropic.{region}.api.aws";
        options.SendOAuthBeta = false;
        options.ApiKey = null;
        options.AuthToken = apiKey;
        options.RequestInterceptor = async (request, ct) =>
        {
            request.Headers.TryAddWithoutValidation(workspaceHeader, workspaceId);
            if (apiKey is null)
            {
                await AwsSigV4.SignAsync(request, credentials ?? AwsCredentials.FromEnvironment(), region, "aws-external-anthropic", null, ct).ConfigureAwait(false);
            }

            return request;
        };
    }

    /// <summary>Creates a client for Claude Platform on AWS.</summary>
    /// <param name="region">The AWS region; defaults to AWS_REGION.</param>
    /// <param name="workspaceId">The workspace id; defaults to ANTHROPIC_AWS_WORKSPACE_ID.</param>
    /// <param name="credentials">SigV4 credentials; defaults to the AWS_* environment variables.</param>
    /// <param name="apiKey">A short-term API key instead of SigV4.</param>
    /// <returns>The client.</returns>
    public static AnthropicApiClient Create(string? region = null, string? workspaceId = null, AwsCredentials? credentials = null, string? apiKey = null)
    {
        var options = new AnthropicClientOptions();
        Configure(options, region, workspaceId, credentials, apiKey);
        return new AnthropicApiClient(options);
    }
}

/// <summary>
/// Google Cloud Vertex AI. Model ids are bare for current models (dated snapshots use <c>@</c>, e.g. <c>claude-opus-4-5@20251101</c>).
/// Requests are rewritten to <c>:rawPredict</c> / <c>:streamRawPredict</c> with <c>anthropic_version: vertex-2023-10-16</c>.
/// Only Messages, streaming and token counting are available.
/// </summary>
public static class AnthropicVertex
{
    /// <summary>Configures options for Vertex AI.</summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="projectId">The GCP project id.</param>
    /// <param name="region">"global" (recommended), a multi-region ("us" / "eu") or a specific region.</param>
    /// <param name="accessTokenProvider">Returns a Google OAuth access token (e.g. from Application Default Credentials).</param>
    public static void Configure(AnthropicClientOptions options, string projectId, string region, Func<CancellationToken, Task<string>> accessTokenProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        ArgumentNullException.ThrowIfNull(accessTokenProvider);

        var host = region switch
        {
            "global" => "aiplatform.googleapis.com",
            "us" or "eu" => $"aiplatform.{region}.rep.googleapis.com",
            _ => $"{region}-aiplatform.googleapis.com",
        };
        options.BaseUrl = $"https://{host}";
        options.SendOAuthBeta = false;
        options.ApiKey = null;
        options.AuthToken = null;
        options.RequestInterceptor = async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var isMessages = path.EndsWith("/v1/messages", StringComparison.Ordinal);
            var isCount = path.EndsWith("/v1/messages/count_tokens", StringComparison.Ordinal);
            if (!isMessages && !isCount)
            {
                throw new NotSupportedException($"'{path}' is not available on Google Vertex AI (only Messages, streaming and token counting are).");
            }

            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct).ConfigureAwait(false))!.AsObject();
            var model = body["model"]?.GetValue<string>() ?? throw new InvalidOperationException("The request has no model.");
            var stream = body["stream"]?.GetValue<bool>() == true;
            body["anthropic_version"] = "vertex-2023-10-16";

            string action;
            if (isCount)
            {
                action = "count-tokens:rawPredict";
            }
            else
            {
                body.Remove("model");
                action = (stream ? "streamRawPredict" : "rawPredict");
            }

            var modelSegment = isCount ? string.Empty : $"/{Uri.EscapeDataString(model)}";
            var url = $"https://{host}/v1/projects/{Uri.EscapeDataString(projectId)}/locations/{Uri.EscapeDataString(region)}/publishers/anthropic/models" + (isCount ? "/" : modelSegment + ":") + action;
            var rewritten = new HttpRequestMessage(request.Method, url)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            foreach (var header in request.Headers)
            {
                if (!string.Equals(header.Key, "anthropic-version", StringComparison.OrdinalIgnoreCase))
                {
                    rewritten.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            rewritten.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await accessTokenProvider(ct).ConfigureAwait(false));
            return rewritten;
        };
    }

    /// <summary>Creates a client for Vertex AI.</summary>
    /// <param name="projectId">The GCP project id.</param>
    /// <param name="region">"global", "us", "eu" or a region.</param>
    /// <param name="accessTokenProvider">Returns a Google OAuth access token.</param>
    /// <returns>The client.</returns>
    public static AnthropicApiClient Create(string projectId, string region, Func<CancellationToken, Task<string>> accessTokenProvider)
    {
        var options = new AnthropicClientOptions();
        Configure(options, projectId, region, accessTokenProvider);
        return new AnthropicApiClient(options);
    }
}

/// <summary>Microsoft Foundry (Azure AI Foundry). Same API shape at <c>https://{resource}.services.ai.azure.com/anthropic</c>.</summary>
public static class AnthropicFoundry
{
    /// <summary>Configures options for Foundry.</summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="resource">The Foundry resource name (or pass <paramref name="baseUrl"/>).</param>
    /// <param name="apiKey">The API key (x-api-key). Defaults to ANTHROPIC_FOUNDRY_API_KEY.</param>
    /// <param name="accessTokenProvider">Alternatively returns a Microsoft Entra access token (bearer).</param>
    /// <param name="baseUrl">The full base URL, e.g. https://res.services.ai.azure.com/anthropic.</param>
    public static void Configure(AnthropicClientOptions options, string? resource = null, string? apiKey = null, Func<CancellationToken, Task<string>>? accessTokenProvider = null, string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        baseUrl ??= resource is null ? Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_BASE_URL") : $"https://{resource}.services.ai.azure.com/anthropic";
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Provide a resource name or base URL.", nameof(resource));
        }

        apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_API_KEY");
        options.BaseUrl = baseUrl.TrimEnd('/');
        options.SendOAuthBeta = false;
        options.AuthToken = null;
        options.ApiKey = accessTokenProvider is null ? apiKey : null;
        options.RequestInterceptor = accessTokenProvider is null
            ? null
            : async (request, ct) =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await accessTokenProvider(ct).ConfigureAwait(false));
                return request;
            };
    }

    /// <summary>Creates a client for Foundry.</summary>
    /// <param name="resource">The Foundry resource name.</param>
    /// <param name="apiKey">The API key; defaults to ANTHROPIC_FOUNDRY_API_KEY.</param>
    /// <param name="accessTokenProvider">Alternatively an Entra token provider.</param>
    /// <param name="baseUrl">The full base URL instead of a resource name.</param>
    /// <returns>The client.</returns>
    public static AnthropicApiClient Create(string? resource = null, string? apiKey = null, Func<CancellationToken, Task<string>>? accessTokenProvider = null, string? baseUrl = null)
    {
        var options = new AnthropicClientOptions();
        Configure(options, resource, apiKey, accessTokenProvider, baseUrl);
        return new AnthropicApiClient(options);
    }
}
