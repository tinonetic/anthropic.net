namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool result content block in a message.
/// </summary>
public class ToolResultContentBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolResultContentBlock"/> class.
    /// </summary>
    /// <param name="toolUseId">The ID of the tool use this result corresponds to.</param>
    /// <param name="content">The result: a string, or a list of <see cref="ContentBlock"/> (text/image/document).</param>
    public ToolResultContentBlock(string toolUseId, object content)
    {
        Type = "tool_result";
        ToolUseId = toolUseId;
        Content = content;
    }

    /// <summary>
    /// Gets or sets the ID of the tool use this result corresponds to.
    /// </summary>
    [JsonPropertyName("tool_use_id")]
    public string ToolUseId { get; set; }

    /// <summary>
    /// Gets or sets the content of the result (string or list of content blocks).
    /// </summary>
    [JsonPropertyName("content")]
    public object Content { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the result is an error.
    /// </summary>
    [JsonPropertyName("is_error")]
    public bool? IsError { get; set; }

    /// <summary>
    /// Gets or sets the prompt-caching breakpoint.
    /// </summary>
    [JsonPropertyName("cache_control")]
    public CacheControl? CacheControl { get; set; }
}
