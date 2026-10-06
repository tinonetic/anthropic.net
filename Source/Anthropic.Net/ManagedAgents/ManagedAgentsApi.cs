namespace Anthropic.Net.ManagedAgents;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

/// <summary>Beta header values used by the Managed Agents API.</summary>
public static class ManagedAgentsBetas
{
    /// <summary>Agents, environments, sessions, vaults, deployments.</summary>
    public const string ManagedAgents = "managed-agents-2026-04-01";

    /// <summary>Memory stores (do not send together with <see cref="ManagedAgents"/>).</summary>
    public const string AgentMemory = "agent-memory-2026-07-22";
}

/// <summary>
/// Shared plumbing for resource clients: path prefix, beta header and the common verbs.
/// </summary>
/// <typeparam name="T">The resource type.</typeparam>
public abstract class ResourceApi<T>
{
    private readonly ApiTransport _transport;
    private readonly string _basePath;
    private readonly string[] _betas;

    internal ResourceApi(ApiTransport transport, string basePath, string beta)
    {
        _transport = transport;
        _basePath = basePath;
        _betas = beta.Length == 0 ? [] : [beta];
    }

    /// <summary>Gets the transport (for derived resource clients).</summary>
    internal ApiTransport Transport => _transport;

    /// <summary>Gets the beta headers for this resource.</summary>
    internal string[] Betas => _betas;

    /// <summary>Creates a resource.</summary>
    /// <param name="body">The request body.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created resource.</returns>
    protected Task<T> CreateCoreAsync(object body, CancellationToken ct) => _transport.RequestAsync<T>(HttpMethod.Post, _basePath, body, _betas, ct);

    /// <summary>Gets a resource.</summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    protected Task<T> GetCoreAsync(string id, CancellationToken ct) => _transport.RequestAsync<T>(HttpMethod.Get, $"{_basePath}/{Uri.EscapeDataString(id)}", null, _betas, ct);

    /// <summary>Updates a resource (POST).</summary>
    /// <param name="id">The id.</param>
    /// <param name="body">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="method">HTTP method (POST by default; PATCH for memories).</param>
    /// <returns>The resource.</returns>
    protected Task<T> UpdateCoreAsync(string id, object body, CancellationToken ct, HttpMethod? method = null)
        => _transport.RequestAsync<T>(method ?? HttpMethod.Post, $"{_basePath}/{Uri.EscapeDataString(id)}", body, _betas, ct);

    /// <summary>Lists resources.</summary>
    /// <param name="query">Pre-built query string.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    protected Task<CursorPage<T>> ListCoreAsync(string query, CancellationToken ct) => _transport.RequestAsync<CursorPage<T>>(HttpMethod.Get, _basePath + query, null, _betas, ct);

    /// <summary>Deletes a resource.</summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    protected Task DeleteCoreAsync(string id, CancellationToken ct) => _transport.RequestAsync(HttpMethod.Delete, $"{_basePath}/{Uri.EscapeDataString(id)}", null, _betas, ct);

    /// <summary>Archives a resource (terminal for most resources: read-only, no unarchive).</summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    protected Task<T> ArchiveCoreAsync(string id, CancellationToken ct) => _transport.RequestAsync<T>(HttpMethod.Post, $"{_basePath}/{Uri.EscapeDataString(id)}/archive", "{}", _betas, ct);

    /// <summary>Posts an action to <c>{base}/{id}/{action}</c>.</summary>
    /// <param name="id">The id.</param>
    /// <param name="action">The action segment.</param>
    /// <param name="body">Optional body.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    protected Task<T> ActionCoreAsync(string id, string action, object? body, CancellationToken ct)
        => _transport.RequestAsync<T>(HttpMethod.Post, $"{_basePath}/{Uri.EscapeDataString(id)}/{action}", body ?? "{}", _betas, ct);
}

/// <summary>Agents: persisted, versioned agent configs. There is no delete - archive is permanent.</summary>
public sealed class AgentsApi : ResourceApi<Agent>
{
    internal AgentsApi(ApiTransport t)
        : base(t, "/v1/agents", ManagedAgentsBetas.ManagedAgents)
    {
    }

    /// <summary>Creates an agent.</summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The agent.</returns>
    public Task<Agent> CreateAsync(CreateAgentRequest request, CancellationToken ct = default) => CreateCoreAsync(request, ct);

    /// <summary>Gets an agent.</summary>
    /// <param name="agentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The agent.</returns>
    public Task<Agent> GetAsync(string agentId, CancellationToken ct = default) => GetCoreAsync(agentId, ct);

    /// <summary>Updates an agent. Include <c>version</c> (&gt;= 1) for optimistic concurrency (mismatch returns 409); omit for last-write-wins.</summary>
    /// <param name="agentId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The agent.</returns>
    public Task<Agent> UpdateAsync(string agentId, object changes, CancellationToken ct = default) => UpdateCoreAsync(agentId, changes, ct);

    /// <summary>Lists agents.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<Agent>> ListAsync(int? limit = null, string? page = null, CancellationToken ct = default)
        => ListCoreAsync(ApiTransport.Query(("limit", limit), ("page", page)), ct);

    /// <summary>Archives an agent. PERMANENT: it becomes read-only and new sessions cannot reference it.</summary>
    /// <param name="agentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The agent.</returns>
    public Task<Agent> ArchiveAsync(string agentId, CancellationToken ct = default) => ArchiveCoreAsync(agentId, ct);

    /// <summary>Lists the versions of an agent.</summary>
    /// <param name="agentId">The id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page of versions.</returns>
    public Task<CursorPage<Agent>> ListVersionsAsync(string agentId, int? limit = null, string? page = null, CancellationToken ct = default)
        => Transport.RequestAsync<CursorPage<Agent>>(HttpMethod.Get, $"/v1/agents/{Uri.EscapeDataString(agentId)}/versions" + ApiTransport.Query(("limit", limit), ("page", page)), null, Betas, ct);
}

/// <summary>Environments: cloud containers or self-hosted sandboxes.</summary>
public sealed class EnvironmentsApi : ResourceApi<AgentEnvironment>
{
    internal EnvironmentsApi(ApiTransport t)
        : base(t, "/v1/environments", ManagedAgentsBetas.ManagedAgents)
    {
    }

    /// <summary>Creates an environment.</summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The environment.</returns>
    public Task<AgentEnvironment> CreateAsync(CreateEnvironmentRequest request, CancellationToken ct = default) => CreateCoreAsync(request, ct);

    /// <summary>Gets an environment.</summary>
    /// <param name="environmentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The environment.</returns>
    public Task<AgentEnvironment> GetAsync(string environmentId, CancellationToken ct = default) => GetCoreAsync(environmentId, ct);

    /// <summary>Updates an environment.</summary>
    /// <param name="environmentId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The environment.</returns>
    public Task<AgentEnvironment> UpdateAsync(string environmentId, object changes, CancellationToken ct = default) => UpdateCoreAsync(environmentId, changes, ct);

    /// <summary>Lists environments.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<AgentEnvironment>> ListAsync(int? limit = null, string? page = null, CancellationToken ct = default)
        => ListCoreAsync(ApiTransport.Query(("limit", limit), ("page", page)), ct);

    /// <summary>Deletes an environment.</summary>
    /// <param name="environmentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string environmentId, CancellationToken ct = default) => DeleteCoreAsync(environmentId, ct);

    /// <summary>Archives an environment (permanent, read-only).</summary>
    /// <param name="environmentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The environment.</returns>
    public Task<AgentEnvironment> ArchiveAsync(string environmentId, CancellationToken ct = default) => ArchiveCoreAsync(environmentId, ct);

    /// <summary>Gets self-hosted work queue depth / pending / workers. Call from outside the worker host.</summary>
    /// <param name="environmentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Raw stats object.</returns>
    public Task<ApiObject> GetWorkStatsAsync(string environmentId, CancellationToken ct = default)
        => Transport.RequestAsync<ApiObject>(HttpMethod.Get, $"/v1/environments/{Uri.EscapeDataString(environmentId)}/work/stats", null, Betas, ct);

    /// <summary>Stops a claimed self-hosted work item.</summary>
    /// <param name="environmentId">The id.</param>
    /// <param name="workId">The work id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task StopWorkAsync(string environmentId, string workId, CancellationToken ct = default)
        => Transport.RequestAsync(HttpMethod.Post, $"/v1/environments/{Uri.EscapeDataString(environmentId)}/work/{Uri.EscapeDataString(workId)}/stop", "{}", Betas, ct);
}

/// <summary>Sessions: runs of an agent, with events, threads and resources.</summary>
public sealed class SessionsApi : ResourceApi<Session>
{
    internal SessionsApi(ApiTransport t)
        : base(t, "/v1/sessions", ManagedAgentsBetas.ManagedAgents)
    {
        Events = new SessionEventsApi(t, "/v1/sessions", Betas);
        Threads = new SessionThreadsApi(t, Betas);
        Resources = new SessionResourcesApi(t, Betas);
    }

    /// <summary>Gets the session events API (list, send, stream).</summary>
    public SessionEventsApi Events { get; }

    /// <summary>Gets the multiagent threads API.</summary>
    public SessionThreadsApi Threads { get; }

    /// <summary>Gets the session resources API.</summary>
    public SessionResourcesApi Resources { get; }

    /// <summary>Creates a session. With <see cref="CreateSessionRequest.InitialEvents"/> the agent loop starts immediately.</summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The session.</returns>
    public Task<Session> CreateAsync(CreateSessionRequest request, CancellationToken ct = default) => CreateCoreAsync(request, ct);

    /// <summary>Gets a session.</summary>
    /// <param name="sessionId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The session.</returns>
    public Task<Session> GetAsync(string sessionId, CancellationToken ct = default) => GetCoreAsync(sessionId, ct);

    /// <summary>Updates metadata/title, session-local agent tools/mcp_servers (session must be idle) or budget (null removes it).</summary>
    /// <param name="sessionId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The session.</returns>
    public Task<Session> UpdateAsync(string sessionId, object changes, CancellationToken ct = default) => UpdateCoreAsync(sessionId, changes, ct);

    /// <summary>Lists sessions.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor (next_page or prev_page).</param>
    /// <param name="order">asc or desc.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<Session>> ListAsync(int? limit = null, string? page = null, string? order = null, CancellationToken ct = default)
        => ListCoreAsync(ApiTransport.Query(("limit", limit), ("page", page), ("order", order)), ct);

    /// <summary>Deletes a session.</summary>
    /// <param name="sessionId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string sessionId, CancellationToken ct = default) => DeleteCoreAsync(sessionId, ct);

    /// <summary>Archives a session.</summary>
    /// <param name="sessionId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The session.</returns>
    public Task<Session> ArchiveAsync(string sessionId, CancellationToken ct = default) => ArchiveCoreAsync(sessionId, ct);
}

/// <summary>Session events: send input, list history, stream live.</summary>
public sealed class SessionEventsApi
{
    private readonly ApiTransport _t;
    private readonly string _base;
    private readonly string[] _betas;

    internal SessionEventsApi(ApiTransport t, string basePath, string[] betas)
    {
        _t = t;
        _base = basePath;
        _betas = betas;
    }

    /// <summary>Sends events (see <see cref="SessionEvents"/>) to a session.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="events">The events.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The accepted events.</returns>
    public Task<ApiObject> SendAsync(string sessionId, IEnumerable<object> events, CancellationToken ct = default)
        => _t.RequestAsync<ApiObject>(HttpMethod.Post, $"{_base}/{Uri.EscapeDataString(sessionId)}/events", new { events }, _betas, ct);

    /// <summary>Lists past events (polling; returns immediately).</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="limit">Page size (default 1000).</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page of events.</returns>
    public Task<CursorPage<SessionEvent>> ListAsync(string sessionId, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<SessionEvent>>(HttpMethod.Get, $"{_base}/{Uri.EscapeDataString(sessionId)}/events" + ApiTransport.Query(("limit", limit), ("page", page)), null, _betas, ct);

    /// <summary>
    /// Streams events live over SSE (long-lived; the server sends heartbeats). Open the stream BEFORE sending the first
    /// message so no events are missed.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="eventDeltas">Optional live previews, e.g. "agent.message" / "agent.thinking".</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The events.</returns>
    public IAsyncEnumerable<SessionEvent> StreamAsync(string sessionId, IEnumerable<string>? eventDeltas = null, CancellationToken ct = default)
    {
        var query = eventDeltas is null ? string.Empty : "?" + string.Join('&', eventDeltas.Select(d => "event_deltas[]=" + Uri.EscapeDataString(d)));
        return _t.StreamSseAsync<SessionEvent>(HttpMethod.Get, $"{_base}/{Uri.EscapeDataString(sessionId)}/events/stream" + query, null, _betas, ct);
    }
}

/// <summary>Per-subagent threads of a multiagent session.</summary>
public sealed class SessionThreadsApi
{
    private readonly ApiTransport _t;
    private readonly string[] _betas;

    internal SessionThreadsApi(ApiTransport t, string[] betas)
    {
        _t = t;
        _betas = betas;
    }

    /// <summary>Lists threads.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListAsync(string sessionId, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<ApiResource>>(HttpMethod.Get, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/threads" + ApiTransport.Query(("limit", limit), ("page", page)), null, _betas, ct);

    /// <summary>Gets a thread.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="threadId">The thread id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The thread.</returns>
    public Task<ApiResource> GetAsync(string sessionId, string threadId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Get, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/threads/{Uri.EscapeDataString(threadId)}", null, _betas, ct);

    /// <summary>Archives a thread (no delete).</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="threadId">The thread id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The thread.</returns>
    public Task<ApiResource> ArchiveAsync(string sessionId, string threadId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/threads/{Uri.EscapeDataString(threadId)}/archive", "{}", _betas, ct);

    /// <summary>Lists past events of one thread.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="threadId">The thread id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page of events.</returns>
    public Task<CursorPage<SessionEvent>> ListEventsAsync(string sessionId, string threadId, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<SessionEvent>>(HttpMethod.Get, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/threads/{Uri.EscapeDataString(threadId)}/events" + ApiTransport.Query(("limit", limit), ("page", page)), null, _betas, ct);

    /// <summary>Streams one thread's events over SSE.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="threadId">The thread id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The events.</returns>
    public IAsyncEnumerable<SessionEvent> StreamEventsAsync(string sessionId, string threadId, CancellationToken ct = default)
        => _t.StreamSseAsync<SessionEvent>(HttpMethod.Get, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/threads/{Uri.EscapeDataString(threadId)}/stream", null, _betas, ct);
}

/// <summary>Resources (files, GitHub repositories) attached to a session.</summary>
public sealed class SessionResourcesApi
{
    private readonly ApiTransport _t;
    private readonly string[] _betas;

    internal SessionResourcesApi(ApiTransport t, string[] betas)
    {
        _t = t;
        _betas = betas;
    }

    /// <summary>Attaches a <c>file</c> or <c>github_repository</c> resource (memory stores attach at create time only).</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="resource">The resource, e.g. <c>new { type = "github_repository", url, authorization_token }</c>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    public Task<ApiResource> AddAsync(string sessionId, object resource, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/resources", resource, _betas, ct);

    /// <summary>Gets a resource.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    public Task<ApiResource> GetAsync(string sessionId, string resourceId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Get, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/resources/{Uri.EscapeDataString(resourceId)}", null, _betas, ct);

    /// <summary>Updates a resource.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The resource.</returns>
    public Task<ApiResource> UpdateAsync(string sessionId, string resourceId, object changes, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/resources/{Uri.EscapeDataString(resourceId)}", changes, _betas, ct);

    /// <summary>Lists resources.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListAsync(string sessionId, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<ApiResource>>(HttpMethod.Get, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/resources", null, _betas, ct);

    /// <summary>Removes a resource from the session.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string sessionId, string resourceId, CancellationToken ct = default)
        => _t.RequestAsync(HttpMethod.Delete, $"/v1/sessions/{Uri.EscapeDataString(sessionId)}/resources/{Uri.EscapeDataString(resourceId)}", null, _betas, ct);
}

/// <summary>Scheduled deployments: cron-fired sessions.</summary>
public sealed class DeploymentsApi : ResourceApi<Deployment>
{
    internal DeploymentsApi(ApiTransport t)
        : base(t, "/v1/deployments", ManagedAgentsBetas.ManagedAgents)
    {
    }

    /// <summary>Creates a deployment.</summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The deployment.</returns>
    public Task<Deployment> CreateAsync(CreateDeploymentRequest request, CancellationToken ct = default) => CreateCoreAsync(request, ct);

    /// <summary>Updates a deployment.</summary>
    /// <param name="deploymentId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The deployment.</returns>
    public Task<Deployment> UpdateAsync(string deploymentId, object changes, CancellationToken ct = default) => UpdateCoreAsync(deploymentId, changes, ct);

    /// <summary>Pauses scheduled triggers (manual runs still work).</summary>
    /// <param name="deploymentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The deployment.</returns>
    public Task<Deployment> PauseAsync(string deploymentId, CancellationToken ct = default) => ActionCoreAsync(deploymentId, "pause", null, ct);

    /// <summary>Resumes from the next occurrence (no backfill).</summary>
    /// <param name="deploymentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The deployment.</returns>
    public Task<Deployment> UnpauseAsync(string deploymentId, CancellationToken ct = default) => ActionCoreAsync(deploymentId, "unpause", null, ct);

    /// <summary>Archives a deployment. TERMINAL: the schedule stops and it becomes immutable.</summary>
    /// <param name="deploymentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The deployment.</returns>
    public Task<Deployment> ArchiveAsync(string deploymentId, CancellationToken ct = default) => ArchiveCoreAsync(deploymentId, ct);

    /// <summary>Triggers a manual run now (works while paused).</summary>
    /// <param name="deploymentId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The run.</returns>
    public Task<DeploymentRun> RunAsync(string deploymentId, CancellationToken ct = default)
        => Transport.RequestAsync<DeploymentRun>(HttpMethod.Post, $"/v1/deployments/{Uri.EscapeDataString(deploymentId)}/run", "{}", Betas, ct);
}

/// <summary>Deployment run records.</summary>
public sealed class DeploymentRunsApi : ResourceApi<DeploymentRun>
{
    internal DeploymentRunsApi(ApiTransport t)
        : base(t, "/v1/deployment_runs", ManagedAgentsBetas.ManagedAgents)
    {
    }

    /// <summary>Lists runs of a deployment.</summary>
    /// <param name="deploymentId">The deployment id.</param>
    /// <param name="hasError">Filter failures (true) or successes (false).</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<DeploymentRun>> ListAsync(string deploymentId, bool? hasError = null, int? limit = null, string? page = null, CancellationToken ct = default)
        => ListCoreAsync(ApiTransport.Query(("deployment_id", deploymentId), ("has_error", hasError), ("limit", limit), ("page", page)), ct);

    /// <summary>Gets a run.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The run.</returns>
    public Task<DeploymentRun> GetAsync(string runId, CancellationToken ct = default) => GetCoreAsync(runId, ct);
}

/// <summary>Vaults: credentials Anthropic stores for you (MCP OAuth/bearer, environment variables).</summary>
public sealed class VaultsApi : ResourceApi<ApiResource>
{
    internal VaultsApi(ApiTransport t)
        : base(t, "/v1/vaults", ManagedAgentsBetas.ManagedAgents)
    {
        Credentials = new CredentialsApi(t);
    }

    /// <summary>Gets the credentials inside vaults.</summary>
    public CredentialsApi Credentials { get; }

    /// <summary>Creates a vault.</summary>
    /// <param name="request">The body, e.g. <c>new { name = "prod" }</c>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The vault.</returns>
    public Task<ApiResource> CreateAsync(object request, CancellationToken ct = default) => CreateCoreAsync(request, ct);

    /// <summary>Gets a vault.</summary>
    /// <param name="vaultId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The vault.</returns>
    public Task<ApiResource> GetAsync(string vaultId, CancellationToken ct = default) => GetCoreAsync(vaultId, ct);

    /// <summary>Updates a vault.</summary>
    /// <param name="vaultId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The vault.</returns>
    public Task<ApiResource> UpdateAsync(string vaultId, object changes, CancellationToken ct = default) => UpdateCoreAsync(vaultId, changes, ct);

    /// <summary>Lists vaults.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListAsync(int? limit = null, string? page = null, CancellationToken ct = default)
        => ListCoreAsync(ApiTransport.Query(("limit", limit), ("page", page)), ct);

    /// <summary>Deletes a vault.</summary>
    /// <param name="vaultId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string vaultId, CancellationToken ct = default) => DeleteCoreAsync(vaultId, ct);

    /// <summary>Archives a vault.</summary>
    /// <param name="vaultId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The vault.</returns>
    public Task<ApiResource> ArchiveAsync(string vaultId, CancellationToken ct = default) => ArchiveCoreAsync(vaultId, ct);
}

/// <summary>Credentials inside a vault.</summary>
public sealed class CredentialsApi
{
    private readonly ApiTransport _t;

    internal CredentialsApi(ApiTransport t) => _t = t;

    /// <summary>Creates a credential.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="credential">The credential body (MCP OAuth, bearer, or environment_variable).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The credential metadata.</returns>
    public Task<ApiResource> CreateAsync(string vaultId, object credential, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, Path(vaultId), credential, Beta, ct);

    /// <summary>Gets credential metadata.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="credentialId">The credential id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The credential metadata.</returns>
    public Task<ApiResource> GetAsync(string vaultId, string credentialId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Get, Path(vaultId, credentialId), null, Beta, ct);

    /// <summary>Updates a credential.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="credentialId">The credential id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The credential metadata.</returns>
    public Task<ApiResource> UpdateAsync(string vaultId, string credentialId, object changes, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, Path(vaultId, credentialId), changes, Beta, ct);

    /// <summary>Lists credentials in a vault.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListAsync(string vaultId, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<ApiResource>>(HttpMethod.Get, Path(vaultId) + ApiTransport.Query(("limit", limit), ("page", page)), null, Beta, ct);

    /// <summary>Deletes a credential.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="credentialId">The credential id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string vaultId, string credentialId, CancellationToken ct = default)
        => _t.RequestAsync(HttpMethod.Delete, Path(vaultId, credentialId), null, Beta, ct);

    /// <summary>Archives a credential.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="credentialId">The credential id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The credential metadata.</returns>
    public Task<ApiResource> ArchiveAsync(string vaultId, string credentialId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, Path(vaultId, credentialId) + "/archive", "{}", Beta, ct);

    /// <summary>Validates an MCP OAuth credential.</summary>
    /// <param name="vaultId">The vault id.</param>
    /// <param name="credentialId">The credential id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    public Task<ApiObject> McpOauthValidateAsync(string vaultId, string credentialId, CancellationToken ct = default)
        => _t.RequestAsync<ApiObject>(HttpMethod.Post, Path(vaultId, credentialId) + "/mcp_oauth_validate", "{}", Beta, ct);

    private static string[] Beta => [ManagedAgentsBetas.ManagedAgents];

    private static string Path(string vaultId, string? credentialId = null)
        => $"/v1/vaults/{Uri.EscapeDataString(vaultId)}/credentials" + (credentialId is null ? string.Empty : "/" + Uri.EscapeDataString(credentialId));
}

/// <summary>Memory stores: workspace-scoped persistent memory (uses the agent-memory beta header).</summary>
public sealed class MemoryStoresApi : ResourceApi<ApiResource>
{
    internal MemoryStoresApi(ApiTransport t)
        : base(t, "/v1/memory_stores", ManagedAgentsBetas.AgentMemory)
    {
        Memories = new MemoriesApi(t);
        Versions = new MemoryVersionsApi(t);
    }

    /// <summary>Gets the memories API.</summary>
    public MemoriesApi Memories { get; }

    /// <summary>Gets the memory versions (audit / rollback) API.</summary>
    public MemoryVersionsApi Versions { get; }

    /// <summary>Creates a store.</summary>
    /// <param name="request">The body: name, description, metadata.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The store.</returns>
    public Task<ApiResource> CreateAsync(object request, CancellationToken ct = default) => CreateCoreAsync(request, ct);

    /// <summary>Gets a store.</summary>
    /// <param name="storeId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The store.</returns>
    public Task<ApiResource> GetAsync(string storeId, CancellationToken ct = default) => GetCoreAsync(storeId, ct);

    /// <summary>Updates a store.</summary>
    /// <param name="storeId">The id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The store.</returns>
    public Task<ApiResource> UpdateAsync(string storeId, object changes, CancellationToken ct = default) => UpdateCoreAsync(storeId, changes, ct);

    /// <summary>Lists stores.</summary>
    /// <param name="includeArchived">Include archived stores.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListAsync(bool? includeArchived = null, int? limit = null, string? page = null, CancellationToken ct = default)
        => ListCoreAsync(ApiTransport.Query(("include_archived", includeArchived), ("limit", limit), ("page", page)), ct);

    /// <summary>Deletes a store.</summary>
    /// <param name="storeId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string storeId, CancellationToken ct = default) => DeleteCoreAsync(storeId, ct);

    /// <summary>Archives a store (read-only, no unarchive).</summary>
    /// <param name="storeId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The store.</returns>
    public Task<ApiResource> ArchiveAsync(string storeId, CancellationToken ct = default) => ArchiveCoreAsync(storeId, ct);
}

/// <summary>Memories: text documents (&lt;= 100KB) inside a store.</summary>
public sealed class MemoriesApi
{
    private readonly ApiTransport _t;

    internal MemoriesApi(ApiTransport t) => _t = t;

    /// <summary>Creates a memory at a path (409 memory_path_conflict_error if occupied).</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="path">The memory path.</param>
    /// <param name="content">The text content.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The memory.</returns>
    public Task<ApiResource> CreateAsync(string storeId, string path, string content, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, Base(storeId), new { path, content }, Beta, ct);

    /// <summary>Gets a memory (view "full" by default).</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="memoryId">The memory id.</param>
    /// <param name="view">"basic" or "full".</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The memory.</returns>
    public Task<ApiResource> GetAsync(string storeId, string memoryId, string? view = null, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Get, Base(storeId) + "/" + Uri.EscapeDataString(memoryId) + ApiTransport.Query(("view", view)), null, Beta, ct);

    /// <summary>Updates content and/or path by id. Pass <c>precondition = new { type = "content_sha256", content_sha256 = ... }</c> for safe concurrent edits.</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="memoryId">The memory id.</param>
    /// <param name="changes">The fields to change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The memory.</returns>
    public Task<ApiResource> UpdateAsync(string storeId, string memoryId, object changes, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Patch, Base(storeId) + "/" + Uri.EscapeDataString(memoryId), changes, Beta, ct);

    /// <summary>Lists memories and prefixes.</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="pathPrefix">Filter by path prefix.</param>
    /// <param name="depth">Prefix depth.</param>
    /// <param name="view">"basic" or "full".</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page (memories and memory prefixes).</returns>
    public Task<CursorPage<ApiResource>> ListAsync(string storeId, string? pathPrefix = null, int? depth = null, string? view = null, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<ApiResource>>(HttpMethod.Get, Base(storeId) + ApiTransport.Query(("path_prefix", pathPrefix), ("depth", depth), ("view", view), ("limit", limit), ("page", page)), null, Beta, ct);

    /// <summary>Deletes a memory.</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="memoryId">The memory id.</param>
    /// <param name="expectedContentSha256">Optional guard against concurrent edits.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string storeId, string memoryId, string? expectedContentSha256 = null, CancellationToken ct = default)
        => _t.RequestAsync(HttpMethod.Delete, Base(storeId) + "/" + Uri.EscapeDataString(memoryId) + ApiTransport.Query(("expected_content_sha256", expectedContentSha256)), null, Beta, ct);

    private static string[] Beta => [ManagedAgentsBetas.AgentMemory];

    private static string Base(string storeId) => $"/v1/memory_stores/{Uri.EscapeDataString(storeId)}/memories";
}

/// <summary>Immutable per-mutation memory snapshots: audit and rollback.</summary>
public sealed class MemoryVersionsApi
{
    private readonly ApiTransport _t;

    internal MemoryVersionsApi(ApiTransport t) => _t = t;

    /// <summary>Lists versions, newest first.</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="memoryId">Filter by memory.</param>
    /// <param name="operation">created, modified or deleted.</param>
    /// <param name="sessionId">Filter by session.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListAsync(string storeId, string? memoryId = null, string? operation = null, string? sessionId = null, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<ApiResource>>(HttpMethod.Get, Base(storeId) + ApiTransport.Query(("memory_id", memoryId), ("operation", operation), ("session_id", sessionId), ("limit", limit), ("page", page)), null, Beta, ct);

    /// <summary>Gets a version with full content.</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="versionId">The version id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The version.</returns>
    public Task<ApiResource> GetAsync(string storeId, string versionId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Get, Base(storeId) + "/" + Uri.EscapeDataString(versionId), null, Beta, ct);

    /// <summary>Redacts a version's content and path (actor and timestamps are preserved).</summary>
    /// <param name="storeId">The store id.</param>
    /// <param name="versionId">The version id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The version.</returns>
    public Task<ApiResource> RedactAsync(string storeId, string versionId, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Post, Base(storeId) + "/" + Uri.EscapeDataString(versionId) + "/redact", "{}", Beta, ct);

    private static string[] Beta => [ManagedAgentsBetas.AgentMemory];

    private static string Base(string storeId) => $"/v1/memory_stores/{Uri.EscapeDataString(storeId)}/memory_versions";
}

/// <summary>Entry point for the Managed Agents API (<c>client.ManagedAgents</c>).</summary>
public sealed class ManagedAgentsApi
{
    internal ManagedAgentsApi(ApiTransport t)
    {
        Agents = new AgentsApi(t);
        Environments = new EnvironmentsApi(t);
        Sessions = new SessionsApi(t);
        Deployments = new DeploymentsApi(t);
        DeploymentRuns = new DeploymentRunsApi(t);
        Vaults = new VaultsApi(t);
        MemoryStores = new MemoryStoresApi(t);
    }

    /// <summary>Gets the agents API.</summary>
    public AgentsApi Agents { get; }

    /// <summary>Gets the environments API.</summary>
    public EnvironmentsApi Environments { get; }

    /// <summary>Gets the sessions API (events, threads, resources).</summary>
    public SessionsApi Sessions { get; }

    /// <summary>Gets the scheduled deployments API.</summary>
    public DeploymentsApi Deployments { get; }

    /// <summary>Gets the deployment runs API.</summary>
    public DeploymentRunsApi DeploymentRuns { get; }

    /// <summary>Gets the vaults API (and credentials).</summary>
    public VaultsApi Vaults { get; }

    /// <summary>Gets the memory stores API (memories, versions).</summary>
    public MemoryStoresApi MemoryStores { get; }
}

/// <summary>Skills API (GA, no beta header): custom skills usable via the <c>container.skills</c> request parameter or on agents.</summary>
public sealed class SkillsApi
{
    private readonly ApiTransport _t;

    internal SkillsApi(ApiTransport t) => _t = t;

    /// <summary>Creates a skill from files (the folder must contain SKILL.md).</summary>
    /// <param name="displayTitle">The display title.</param>
    /// <param name="files">Files as (path inside the skill folder, content).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The skill.</returns>
    public Task<Skill> CreateAsync(string displayTitle, IEnumerable<(string Path, byte[] Content)> files, CancellationToken ct = default)
        => Upload<Skill>("/v1/skills", displayTitle, files, ct);

    /// <summary>Lists skills.</summary>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="source">Filter: custom or anthropic.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<Skill>> ListAsync(int? limit = null, string? page = null, string? source = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<Skill>>(HttpMethod.Get, "/v1/skills" + ApiTransport.Query(("limit", limit), ("page", page), ("source", source)), null, null, ct);

    /// <summary>Gets a skill.</summary>
    /// <param name="skillId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The skill.</returns>
    public Task<Skill> GetAsync(string skillId, CancellationToken ct = default)
        => _t.RequestAsync<Skill>(HttpMethod.Get, $"/v1/skills/{Uri.EscapeDataString(skillId)}", null, null, ct);

    /// <summary>Deletes a skill (delete its versions first).</summary>
    /// <param name="skillId">The id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteAsync(string skillId, CancellationToken ct = default)
        => _t.RequestAsync(HttpMethod.Delete, $"/v1/skills/{Uri.EscapeDataString(skillId)}", null, null, ct);

    /// <summary>Creates a new version of a skill.</summary>
    /// <param name="skillId">The skill id.</param>
    /// <param name="files">Files as (path inside the skill folder, content).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The version.</returns>
    public Task<ApiResource> CreateVersionAsync(string skillId, IEnumerable<(string Path, byte[] Content)> files, CancellationToken ct = default)
        => Upload<ApiResource>($"/v1/skills/{Uri.EscapeDataString(skillId)}/versions", null, files, ct);

    /// <summary>Lists the versions of a skill.</summary>
    /// <param name="skillId">The skill id.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="page">Cursor.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A page.</returns>
    public Task<CursorPage<ApiResource>> ListVersionsAsync(string skillId, int? limit = null, string? page = null, CancellationToken ct = default)
        => _t.RequestAsync<CursorPage<ApiResource>>(HttpMethod.Get, $"/v1/skills/{Uri.EscapeDataString(skillId)}/versions" + ApiTransport.Query(("limit", limit), ("page", page)), null, null, ct);

    /// <summary>Gets a skill version.</summary>
    /// <param name="skillId">The skill id.</param>
    /// <param name="version">The version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The version.</returns>
    public Task<ApiResource> GetVersionAsync(string skillId, string version, CancellationToken ct = default)
        => _t.RequestAsync<ApiResource>(HttpMethod.Get, $"/v1/skills/{Uri.EscapeDataString(skillId)}/versions/{Uri.EscapeDataString(version)}", null, null, ct);

    /// <summary>Deletes a skill version.</summary>
    /// <param name="skillId">The skill id.</param>
    /// <param name="version">The version.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task.</returns>
    public Task DeleteVersionAsync(string skillId, string version, CancellationToken ct = default)
        => _t.RequestAsync(HttpMethod.Delete, $"/v1/skills/{Uri.EscapeDataString(skillId)}/versions/{Uri.EscapeDataString(version)}", null, null, ct);

    private async Task<T> Upload<T>(string path, string? displayTitle, IEnumerable<(string Path, byte[] Content)> files, CancellationToken ct)
    {
        var list = files.ToList();
        using var response = await _t.SendAsync(
            () =>
            {
                var form = new MultipartFormDataContent();
                if (displayTitle is not null)
                {
                    form.Add(new StringContent(displayTitle), "display_title");
                }

                foreach (var (filePath, content) in list)
                {
                    var part = new ByteArrayContent(content);
                    part.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    form.Add(part, "files[]", filePath);
                }

                var message = new HttpRequestMessage(HttpMethod.Post, _t.BaseUrl + path) { Content = form };
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                return message;
            },
            null,
            HttpCompletionOption.ResponseContentRead,
            ct).ConfigureAwait(false);
        return ApiTransport.Deserialize<T>(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
    }
}
