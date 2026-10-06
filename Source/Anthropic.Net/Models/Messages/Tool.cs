namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool that can be used by Claude: a user-defined (client) tool, or an Anthropic-defined
/// server tool created with <see cref="WebSearch"/>, <see cref="WebFetch"/>, <see cref="CodeExecution"/> etc.
/// </summary>
public class Tool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tool"/> class (user-defined tool).
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="description">The description of the tool.</param>
    /// <param name="inputSchema">The input schema for the tool.</param>
    public Tool(string name, string description, object inputSchema)
    {
        Name = name;
        Description = description;
        InputSchema = inputSchema;
    }

    private Tool(string type, string name)
    {
        Type = type;
        Name = name;
    }

    /// <summary>Gets or sets the tool type (null for user-defined tools; e.g. "web_search_20260209" for server tools).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the tool.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the input schema for the tool.
    /// </summary>
    [JsonPropertyName("input_schema")]
    public object? InputSchema { get; set; }

    /// <summary>Gets or sets a value indicating whether tool inputs are guaranteed to match the schema (needs additionalProperties:false + required).</summary>
    [JsonPropertyName("strict")]
    public bool? Strict { get; set; }

    /// <summary>Gets or sets a value indicating whether to stream large tool inputs as generated (fine-grained tool streaming).</summary>
    [JsonPropertyName("eager_input_streaming")]
    public bool? EagerInputStreaming { get; set; }

    /// <summary>Gets or sets a value indicating whether the tool is loaded on demand by tool search.</summary>
    [JsonPropertyName("defer_loading")]
    public bool? DeferLoading { get; set; }

    /// <summary>Gets or sets which callers may invoke the tool (programmatic tool calling).</summary>
    [JsonPropertyName("allowed_callers")]
    public IList<string>? AllowedCallers { get; set; }

    /// <summary>Gets or sets the prompt-caching breakpoint.</summary>
    [JsonPropertyName("cache_control")]
    public CacheControl? CacheControl { get; set; }

    /// <summary>Gets or sets the max uses per request (web_search / web_fetch).</summary>
    [JsonPropertyName("max_uses")]
    public int? MaxUses { get; set; }

    /// <summary>Gets or sets the allowed domains (web_search / web_fetch). Mutually exclusive with blocked domains.</summary>
    [JsonPropertyName("allowed_domains")]
    public IList<string>? AllowedDomains { get; set; }

    /// <summary>Gets or sets the blocked domains (web_search / web_fetch).</summary>
    [JsonPropertyName("blocked_domains")]
    public IList<string>? BlockedDomains { get; set; }

    /// <summary>Gets or sets the approximate user location (web_search), e.g. { type = "approximate", city = "Paris" }.</summary>
    [JsonPropertyName("user_location")]
    public object? UserLocation { get; set; }

    /// <summary>Gets or sets citations settings (web_fetch).</summary>
    [JsonPropertyName("citations")]
    public CitationsConfig? Citations { get; set; }

    /// <summary>Gets or sets the max content tokens (web_fetch).</summary>
    [JsonPropertyName("max_content_tokens")]
    public int? MaxContentTokens { get; set; }

    /// <summary>Gets or sets the MCP server name (mcp_toolset).</summary>
    [JsonPropertyName("mcp_server_name")]
    public string? McpServerName { get; set; }

    /// <summary>Creates the web search server tool (dynamic filtering variant; use "web_search_20250305" for older models).</summary>
    /// <param name="maxUses">Optional max searches per request.</param>
    /// <param name="type">Tool version type.</param>
    /// <returns>The tool.</returns>
    public static Tool WebSearch(int? maxUses = null, string type = "web_search_20260209") => new(type, "web_search") { MaxUses = maxUses };

    /// <summary>Creates the web fetch server tool (use "web_fetch_20250910" for older models).</summary>
    /// <param name="maxUses">Optional max fetches per request.</param>
    /// <param name="type">Tool version type.</param>
    /// <returns>The tool.</returns>
    public static Tool WebFetch(int? maxUses = null, string type = "web_fetch_20260209") => new(type, "web_fetch") { MaxUses = maxUses };

    /// <summary>Creates the code execution server tool.</summary>
    /// <param name="type">Tool version type.</param>
    /// <returns>The tool.</returns>
    public static Tool CodeExecution(string type = "code_execution_20260521") => new(type, "code_execution");

    /// <summary>Creates the memory tool (client-executed).</summary>
    /// <returns>The tool.</returns>
    public static Tool Memory() => new("memory_20250818", "memory");

    /// <summary>Creates the bash tool (client-executed, schema-less).</summary>
    /// <returns>The tool.</returns>
    public static Tool Bash() => new("bash_20250124", "bash");

    /// <summary>Creates the text editor tool (client-executed, schema-less).</summary>
    /// <returns>The tool.</returns>
    public static Tool TextEditor() => new("text_editor_20250728", "str_replace_based_edit_tool");

    /// <summary>Creates the regex tool-search tool; mark other tools with <see cref="DeferLoading"/>.</summary>
    /// <returns>The tool.</returns>
    public static Tool ToolSearchRegex() => new("tool_search_tool_regex_20251119", "tool_search_tool_regex");

    /// <summary>Creates the BM25 tool-search tool.</summary>
    /// <returns>The tool.</returns>
    public static Tool ToolSearchBm25() => new("tool_search_tool_bm25_20251119", "tool_search_tool_bm25");

    /// <summary>Creates the computer-use toolset (Opus 5.5 / Sonnet 5.5 on the Claude API).</summary>
    /// <returns>The tool.</returns>
    public static Tool ComputerToolset() => new("computer_toolset_20260801", "computer");

    /// <summary>Creates an MCP toolset exposing the tools of an <see cref="McpServer"/> declared on the request.</summary>
    /// <param name="serverName">Name of the MCP server.</param>
    /// <returns>The tool.</returns>
    public static Tool McpToolset(string serverName) => new("mcp_toolset", serverName) { McpServerName = serverName };
}

/// <summary>An MCP server for the MCP connector (needs beta mcp-client-2025-11-20 and a matching <see cref="Tool.McpToolset"/>).</summary>
public class McpServer
{
    /// <summary>Gets or sets the type; always "url".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "url";

    /// <summary>Gets or sets the server URL.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the server name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth bearer token.</summary>
    [JsonPropertyName("authorization_token")]
    public string? AuthorizationToken { get; set; }
}

/// <summary>Tool choice (<c>tool_choice</c>). Note: any/tool are rejected by Fable 5.1, Opus 5.5 and Sonnet 5.5.</summary>
public class ToolChoice
{
    /// <summary>Gets or sets the type: auto, any, tool, none.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "auto";

    /// <summary>Gets or sets the tool name (type "tool").</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Gets or sets a value indicating whether parallel tool use is disabled.</summary>
    [JsonPropertyName("disable_parallel_tool_use")]
    public bool? DisableParallelToolUse { get; set; }

    /// <summary>Claude decides.</summary>
    /// <param name="disableParallel">Disable parallel tool use.</param>
    /// <returns>The choice.</returns>
    public static ToolChoice Auto(bool? disableParallel = null) => new() { Type = "auto", DisableParallelToolUse = disableParallel };

    /// <summary>Claude must use some tool.</summary>
    /// <param name="disableParallel">Disable parallel tool use.</param>
    /// <returns>The choice.</returns>
    public static ToolChoice Any(bool? disableParallel = null) => new() { Type = "any", DisableParallelToolUse = disableParallel };

    /// <summary>Claude must use the named tool.</summary>
    /// <param name="name">Tool name.</param>
    /// <param name="disableParallel">Disable parallel tool use.</param>
    /// <returns>The choice.</returns>
    public static ToolChoice ForTool(string name, bool? disableParallel = null) => new() { Type = "tool", Name = name, DisableParallelToolUse = disableParallel };

    /// <summary>Claude must not use tools.</summary>
    /// <returns>The choice.</returns>
    public static ToolChoice None() => new() { Type = "none" };
}
