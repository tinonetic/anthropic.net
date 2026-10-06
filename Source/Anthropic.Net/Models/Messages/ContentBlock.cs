namespace Anthropic.Net.Models.Messages;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Base class for content blocks in messages. Unknown block types deserialize to this base class
/// (their raw fields are kept in <see cref="ExtensionData"/>) so new API block types never break parsing.
/// </summary>
[JsonConverter(typeof(ContentBlockConverter))]
public class ContentBlock
{
    /// <summary>
    /// Gets or sets the type of content block (the wire discriminator; empty for unknown block types).
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any fields not modelled by the typed block (forward compatibility).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Prompt-caching breakpoint (<c>cache_control</c>).</summary>
public class CacheControl
{
    /// <summary>Gets or sets the cache type; always "ephemeral".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ephemeral";

    /// <summary>Gets or sets the optional TTL: "5m" (default) or "1h".</summary>
    [JsonPropertyName("ttl")]
    public string? Ttl { get; set; }

    /// <summary>Creates an ephemeral cache breakpoint.</summary>
    /// <param name="ttl">Optional TTL, "5m" or "1h".</param>
    /// <returns>The cache control.</returns>
    public static CacheControl Ephemeral(string? ttl = null) => new() { Ttl = ttl };
}

/// <summary>A thinking block returned when extended/adaptive thinking is enabled. Echo it back unchanged.</summary>
public class ThinkingContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="ThinkingContentBlock"/> class.</summary>
    public ThinkingContentBlock() => Type = "thinking";

    /// <summary>Gets or sets the thinking text (summary, or empty when display is omitted).</summary>
    [JsonPropertyName("thinking")]
    public string Thinking { get; set; } = string.Empty;

    /// <summary>Gets or sets the signature that must be passed back unchanged.</summary>
    [JsonPropertyName("signature")]
    public string? Signature { get; set; }
}

/// <summary>A redacted thinking block. Pass back unchanged.</summary>
public class RedactedThinkingContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="RedactedThinkingContentBlock"/> class.</summary>
    public RedactedThinkingContentBlock() => Type = "redacted_thinking";

    /// <summary>Gets or sets the opaque data.</summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;
}

/// <summary>A server-side tool invocation (web_search, web_fetch, code_execution, ...).</summary>
public class ServerToolUseContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="ServerToolUseContentBlock"/> class.</summary>
    public ServerToolUseContentBlock() => Type = "server_tool_use";

    /// <summary>Gets or sets the id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the tool name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the tool input.</summary>
    [JsonPropertyName("input")]
    public JsonElement Input { get; set; }
}

/// <summary>Base for server-tool result blocks: a tool_use_id plus a result object/array (errors are returned here with HTTP 200).</summary>
public abstract class ServerToolResultContentBlock : ContentBlock
{
    /// <summary>Gets or sets the id of the server_tool_use this answers.</summary>
    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw result. For web search success this is an array; on error an object with error_code.</summary>
    [JsonPropertyName("content")]
    public JsonElement Content { get; set; }
}

/// <summary>Web search result block.</summary>
public class WebSearchToolResultContentBlock : ServerToolResultContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="WebSearchToolResultContentBlock"/> class.</summary>
    public WebSearchToolResultContentBlock() => Type = "web_search_tool_result";
}

/// <summary>Web fetch result block.</summary>
public class WebFetchToolResultContentBlock : ServerToolResultContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="WebFetchToolResultContentBlock"/> class.</summary>
    public WebFetchToolResultContentBlock() => Type = "web_fetch_tool_result";
}

/// <summary>Code execution result block.</summary>
public class CodeExecutionToolResultContentBlock : ServerToolResultContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="CodeExecutionToolResultContentBlock"/> class.</summary>
    public CodeExecutionToolResultContentBlock() => Type = "code_execution_tool_result";
}

/// <summary>Bash code execution result block (stdout/stderr/return_code).</summary>
public class BashCodeExecutionToolResultContentBlock : ServerToolResultContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="BashCodeExecutionToolResultContentBlock"/> class.</summary>
    public BashCodeExecutionToolResultContentBlock() => Type = "bash_code_execution_tool_result";
}

/// <summary>Text editor code execution result block.</summary>
public class TextEditorCodeExecutionToolResultContentBlock : ServerToolResultContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="TextEditorCodeExecutionToolResultContentBlock"/> class.</summary>
    public TextEditorCodeExecutionToolResultContentBlock() => Type = "text_editor_code_execution_tool_result";
}

/// <summary>Tool search result block.</summary>
public class ToolSearchToolResultContentBlock : ServerToolResultContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="ToolSearchToolResultContentBlock"/> class.</summary>
    public ToolSearchToolResultContentBlock() => Type = "tool_search_tool_result";
}

/// <summary>An MCP connector tool call.</summary>
public class McpToolUseContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="McpToolUseContentBlock"/> class.</summary>
    public McpToolUseContentBlock() => Type = "mcp_tool_use";

    /// <summary>Gets or sets the id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the tool name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the MCP server name.</summary>
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the input.</summary>
    [JsonPropertyName("input")]
    public JsonElement Input { get; set; }
}

/// <summary>An MCP connector tool result.</summary>
public class McpToolResultContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="McpToolResultContentBlock"/> class.</summary>
    public McpToolResultContentBlock() => Type = "mcp_tool_result";

    /// <summary>Gets or sets the id of the mcp_tool_use.</summary>
    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the call errored.</summary>
    [JsonPropertyName("is_error")]
    public bool IsError { get; set; }

    /// <summary>Gets or sets the content (string or blocks).</summary>
    [JsonPropertyName("content")]
    public JsonElement Content { get; set; }
}

/// <summary>A file uploaded into the code execution container.</summary>
public class ContainerUploadContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="ContainerUploadContentBlock"/> class.</summary>
    public ContainerUploadContentBlock() => Type = "container_upload";

    /// <summary>Gets or sets the Files API id.</summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;
}

/// <summary>A server-side compaction summary. Append it back unchanged to keep compaction state.</summary>
public class CompactionContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="CompactionContentBlock"/> class.</summary>
    public CompactionContentBlock() => Type = "compaction";

    /// <summary>Gets or sets the summary content.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>A document (PDF / plain text / custom content) content block, optionally with citations.</summary>
public class DocumentContentBlock : ContentBlock
{
    /// <summary>Initializes a new instance of the <see cref="DocumentContentBlock"/> class.</summary>
    public DocumentContentBlock() => Type = "document";

    /// <summary>Gets or sets the source: base64 / text / url / content / file.</summary>
    [JsonPropertyName("source")]
    public DocumentSource? Source { get; set; }

    /// <summary>Gets or sets the title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets free-form context not cited.</summary>
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    /// <summary>Gets or sets citations settings, e.g. { enabled = true }.</summary>
    [JsonPropertyName("citations")]
    public CitationsConfig? Citations { get; set; }

    /// <summary>Gets or sets the cache breakpoint.</summary>
    [JsonPropertyName("cache_control")]
    public CacheControl? CacheControl { get; set; }

    /// <summary>Creates a base64 PDF document block.</summary>
    /// <param name="pdf">PDF bytes.</param>
    /// <param name="citations">Enable citations.</param>
    /// <returns>The block.</returns>
    public static DocumentContentBlock FromPdf(byte[] pdf, bool citations = false) => new()
    {
        Source = new DocumentSource { Type = "base64", MediaType = "application/pdf", Data = Convert.ToBase64String(pdf) },
        Citations = citations ? new CitationsConfig { Enabled = true } : null,
    };

    /// <summary>Creates a plain text document block.</summary>
    /// <param name="text">Document text.</param>
    /// <param name="title">Optional title.</param>
    /// <param name="citations">Enable citations.</param>
    /// <returns>The block.</returns>
    public static DocumentContentBlock FromText(string text, string? title = null, bool citations = false) => new()
    {
        Source = new DocumentSource { Type = "text", MediaType = "text/plain", Data = text },
        Title = title,
        Citations = citations ? new CitationsConfig { Enabled = true } : null,
    };

    /// <summary>Creates a block referencing a PDF by URL.</summary>
    /// <param name="url">The URL.</param>
    /// <returns>The block.</returns>
    public static DocumentContentBlock FromUrl(string url) => new() { Source = new DocumentSource { Type = "url", Url = url } };

    /// <summary>Creates a block referencing a Files API upload.</summary>
    /// <param name="fileId">The file id.</param>
    /// <returns>The block.</returns>
    public static DocumentContentBlock FromFileId(string fileId) => new() { Source = new DocumentSource { Type = "file", FileId = fileId } };
}

/// <summary>Document source.</summary>
public class DocumentSource
{
    /// <summary>Gets or sets the source type: base64, text, url, content, file.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the media type.</summary>
    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    /// <summary>Gets or sets the data (base64 or text).</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>Gets or sets the URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Gets or sets the Files API id.</summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; set; }

    /// <summary>Gets or sets custom content blocks (type "content").</summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

/// <summary>Citations settings for a document.</summary>
public class CitationsConfig
{
    /// <summary>Gets or sets a value indicating whether citations are enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}
