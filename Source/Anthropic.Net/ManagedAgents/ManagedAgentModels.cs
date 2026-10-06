namespace Anthropic.Net.ManagedAgents;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Base for API objects that must stay forward compatible: unknown fields are kept in
/// <see cref="AdditionalProperties"/> on responses and are sent on requests.
/// </summary>
public class ApiObject
{
    /// <summary>Gets or sets fields not modelled by this SDK version.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    /// <summary>Reads an additional property as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="name">The JSON property name.</param>
    /// <returns>The value, or default when absent.</returns>
    public T? GetProperty<T>(string name)
        => AdditionalProperties is not null && AdditionalProperties.TryGetValue(name, out var v) ? v.Deserialize<T>(AnthropicJson.Options) : default;
}

/// <summary>A resource returned by the API: <c>id</c>, <c>type</c>, timestamps and metadata plus a few commonly used fields.</summary>
public class ApiResource : ApiObject
{
    /// <summary>Gets or sets the id (agent_, env_, sesn_, depl_, drun_, vlt_, ...).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the object type.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Gets or sets the name, where the resource has one.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the creation time.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the last update time.</summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the archive time (archived resources are read-only; there is no unarchive).</summary>
    [JsonPropertyName("archived_at")]
    public DateTimeOffset? ArchivedAt { get; set; }

    /// <summary>Gets or sets user metadata (max 16 pairs).</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>A saved agent configuration (model, system prompt, tools, skills, MCP servers).</summary>
public class Agent : ApiResource
{
    /// <summary>Gets or sets the version (pin sessions to a version).</summary>
    [JsonPropertyName("version")]
    public int? Version { get; set; }

    /// <summary>Gets or sets the model (string id or object).</summary>
    [JsonPropertyName("model")]
    public JsonElement? Model { get; set; }

    /// <summary>Gets or sets the system prompt.</summary>
    [JsonPropertyName("system")]
    public string? System { get; set; }

    /// <summary>Gets or sets the tools.</summary>
    [JsonPropertyName("tools")]
    public JsonElement? Tools { get; set; }
}

/// <summary>An environment (cloud container or self-hosted) that sessions run in.</summary>
public class AgentEnvironment : ApiResource
{
    /// <summary>Gets or sets the config (type, networking, packages).</summary>
    [JsonPropertyName("config")]
    public JsonElement? Config { get; set; }
}

/// <summary>A session: one run of an agent in an environment.</summary>
public class Session : ApiResource
{
    /// <summary>Gets or sets the status: idle, running, terminated, ...</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Gets or sets the title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets the environment id.</summary>
    [JsonPropertyName("environment_id")]
    public string? EnvironmentId { get; set; }

    /// <summary>Gets or sets the agent snapshot.</summary>
    [JsonPropertyName("agent")]
    public JsonElement? Agent { get; set; }

    /// <summary>Gets or sets cumulative usage.</summary>
    [JsonPropertyName("usage")]
    public JsonElement? Usage { get; set; }
}

/// <summary>A scheduled deployment (cron-fired sessions).</summary>
public class Deployment : ApiResource
{
    /// <summary>Gets or sets the status: active, paused, archived.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Gets or sets why the deployment is paused.</summary>
    [JsonPropertyName("paused_reason")]
    public string? PausedReason { get; set; }

    /// <summary>Gets or sets the schedule (expression, timezone, upcoming_runs_at).</summary>
    [JsonPropertyName("schedule")]
    public JsonElement? Schedule { get; set; }
}

/// <summary>One trigger attempt of a deployment.</summary>
public class DeploymentRun : ApiResource
{
    /// <summary>Gets or sets the deployment id.</summary>
    [JsonPropertyName("deployment_id")]
    public string? DeploymentId { get; set; }

    /// <summary>Gets or sets the created session id.</summary>
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    /// <summary>Gets or sets the error (environment_archived, agent_archived, vault_not_found, session_rate_limited, service_unavailable).</summary>
    [JsonPropertyName("error")]
    public JsonElement? Error { get; set; }
}

/// <summary>A session or thread event (user.message, agent.message, session.status_idle, ...).</summary>
public class SessionEvent : ApiObject
{
    /// <summary>Gets or sets the event id (persisted events only).</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Gets or sets the dotted event type, e.g. agent.message.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets when processing finished (null while queued).</summary>
    [JsonPropertyName("processed_at")]
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Gets or sets the content blocks, for message events.</summary>
    [JsonPropertyName("content")]
    public JsonElement? Content { get; set; }

    /// <summary>Gets the concatenated text of the text content blocks.</summary>
    [JsonIgnore]
    public string Text => Content is { ValueKind: JsonValueKind.Array } c
        ? string.Concat(c.EnumerateArray().Where(b => b.TryGetProperty("type", out var t) && t.GetString() == "text" && b.TryGetProperty("text", out _)).Select(b => b.GetProperty("text").GetString()))
        : string.Empty;
}

/// <summary>Builders for the events you send to a session.</summary>
public static class SessionEvents
{
    /// <summary>A user message.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The event.</returns>
    public static object UserMessage(string text) => new { type = "user.message", content = new[] { new { type = "text", text } } };

    /// <summary>A system message (appends operator context; model gated).</summary>
    /// <param name="text">The text.</param>
    /// <returns>The event.</returns>
    public static object SystemMessage(string text) => new { type = "system.message", content = new[] { new { type = "text", text } } };

    /// <summary>Interrupts the running agent.</summary>
    /// <returns>The event.</returns>
    public static object Interrupt() => new { type = "user.interrupt" };

    /// <summary>The result of a custom tool call.</summary>
    /// <param name="customToolUseId">The id of the custom tool use event.</param>
    /// <param name="text">The result text.</param>
    /// <param name="isError">Whether the tool failed.</param>
    /// <returns>The event.</returns>
    public static object CustomToolResult(string customToolUseId, string text, bool isError = false)
        => new { type = "user.custom_tool_result", custom_tool_use_id = customToolUseId, content = new[] { new { type = "text", text } }, is_error = isError };

    /// <summary>Starts a rubric-graded outcome loop.</summary>
    /// <param name="description">What to produce.</param>
    /// <param name="rubric">The rubric: <c>new { type = "text", content = "..." }</c> or <c>new { type = "file", file_id = "..." }</c>.</param>
    /// <param name="maxIterations">Max iterations (default 3, max 20).</param>
    /// <returns>The event.</returns>
    public static object DefineOutcome(string description, object rubric, int? maxIterations = null)
        => new { type = "user.define_outcome", description, rubric, max_iterations = maxIterations };
}

/// <summary>A page of a page/next_page cursor-paginated list (Managed Agents, Skills).</summary>
/// <typeparam name="T">The item type.</typeparam>
public class CursorPage<T>
{
    /// <summary>Gets or sets the items.</summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];

    /// <summary>Gets or sets the cursor for the next page (null at the end).</summary>
    [JsonPropertyName("next_page")]
    public string? NextPage { get; set; }

    /// <summary>Gets or sets the cursor for the previous page (sessions only).</summary>
    [JsonPropertyName("prev_page")]
    public string? PrevPage { get; set; }

    /// <summary>Gets or sets whether more results exist (some endpoints).</summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }
}

/// <summary>Request to create an agent.</summary>
public class CreateAgentRequest : ApiObject
{
    /// <summary>Initializes a new instance of the <see cref="CreateAgentRequest"/> class.</summary>
    /// <param name="name">Agent name (1-256 chars).</param>
    /// <param name="model">Model id string, or an object <c>{ id, speed?, effort?, inference_geo? }</c>.</param>
    public CreateAgentRequest(string name, object model)
    {
        Name = name;
        Model = model;
    }

    /// <summary>Gets or sets the name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>Gets or sets the model (string or object).</summary>
    [JsonPropertyName("model")]
    public object Model { get; set; }

    /// <summary>Gets or sets the description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the system prompt (up to 100,000 chars).</summary>
    [JsonPropertyName("system")]
    public string? System { get; set; }

    /// <summary>Gets or sets tools, e.g. <c>new { type = "agent_toolset_20260401" }</c> plus custom tools.</summary>
    [JsonPropertyName("tools")]
    public IList<object>? Tools { get; set; }

    /// <summary>Gets or sets skills, e.g. <c>{ type = "anthropic", skill_id = "xlsx" }</c>.</summary>
    [JsonPropertyName("skills")]
    public IList<object>? Skills { get; set; }

    /// <summary>Gets or sets MCP servers (unique names).</summary>
    [JsonPropertyName("mcp_servers")]
    public IList<object>? McpServers { get; set; }

    /// <summary>Gets or sets multiagent config, e.g. <c>{ type = "coordinator", agents = [...] }</c>.</summary>
    [JsonPropertyName("multiagent")]
    public object? Multiagent { get; set; }

    /// <summary>Gets or sets metadata.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>Request to create an environment.</summary>
public class CreateEnvironmentRequest : ApiObject
{
    /// <summary>Initializes a new instance of the <see cref="CreateEnvironmentRequest"/> class.</summary>
    /// <param name="name">Environment name.</param>
    /// <param name="config">Config, e.g. <c>new { type = "cloud", networking = new { type = "unrestricted" } }</c> or <c>new { type = "self_hosted" }</c>.</param>
    public CreateEnvironmentRequest(string name, object config)
    {
        Name = name;
        Config = config;
    }

    /// <summary>Gets or sets the name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>Gets or sets the config.</summary>
    [JsonPropertyName("config")]
    public object Config { get; set; }

    /// <summary>Gets or sets the description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets metadata.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>Request to create a session (also the session template of a deployment).</summary>
public class CreateSessionRequest : ApiObject
{
    /// <summary>Initializes a new instance of the <see cref="CreateSessionRequest"/> class.</summary>
    /// <param name="agent">Agent id (latest version), or <c>{ type = "agent", id, version }</c> / <c>{ type = "agent_with_overrides", ... }</c>.</param>
    /// <param name="environmentId">The environment id.</param>
    public CreateSessionRequest(object agent, string environmentId)
    {
        Agent = agent;
        EnvironmentId = environmentId;
    }

    /// <summary>Gets or sets the agent reference.</summary>
    [JsonPropertyName("agent")]
    public object Agent { get; set; }

    /// <summary>Gets or sets the environment id.</summary>
    [JsonPropertyName("environment_id")]
    public string EnvironmentId { get; set; }

    /// <summary>Gets or sets the title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets resources (github_repository, file, memory_store).</summary>
    [JsonPropertyName("resources")]
    public IList<object>? Resources { get; set; }

    /// <summary>Gets or sets events sent at creation (max 50; user.message / user.define_outcome only) - starts the agent loop.</summary>
    [JsonPropertyName("initial_events")]
    public IList<object>? InitialEvents { get; set; }

    /// <summary>Gets or sets vault ids (MCP credentials, environment variables).</summary>
    [JsonPropertyName("vault_ids")]
    public IList<string>? VaultIds { get; set; }

    /// <summary>Gets or sets a hard spend cap, e.g. <c>{ type = "limit", max_list_cost = { amount = "2500", currency = "USD" } }</c>.</summary>
    [JsonPropertyName("budget")]
    public object? Budget { get; set; }

    /// <summary>Gets or sets metadata.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>Request to create a scheduled deployment.</summary>
public class CreateDeploymentRequest : CreateSessionRequest
{
    /// <summary>Initializes a new instance of the <see cref="CreateDeploymentRequest"/> class.</summary>
    /// <param name="name">Deployment name.</param>
    /// <param name="agent">Agent reference.</param>
    /// <param name="environmentId">The environment id.</param>
    /// <param name="schedule">Schedule, e.g. <c>new { type = "cron", expression = "0 20 * * 5", timezone = "America/New_York" }</c>.</param>
    public CreateDeploymentRequest(string name, object agent, string environmentId, object schedule)
        : base(agent, environmentId)
    {
        Name = name;
        Schedule = schedule;
    }

    /// <summary>Gets or sets the name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>Gets or sets the schedule.</summary>
    [JsonPropertyName("schedule")]
    public object Schedule { get; set; }
}

/// <summary>A Skills API skill.</summary>
public class Skill : ApiResource
{
    /// <summary>Gets or sets the display title.</summary>
    [JsonPropertyName("display_title")]
    public string? DisplayTitle { get; set; }

    /// <summary>Gets or sets the source: custom or anthropic.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>Gets or sets the latest version.</summary>
    [JsonPropertyName("latest_version")]
    public string? LatestVersion { get; set; }
}
