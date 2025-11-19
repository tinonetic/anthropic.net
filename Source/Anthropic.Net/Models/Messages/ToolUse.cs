namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool use in a message.
/// </summary>
public class ToolUse
{
    /// <summary>
    /// Gets or sets the type of the content block.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "tool_use";

    /// <summary>
    /// Gets or sets the tool call.
    /// </summary>
    [JsonPropertyName("tool_use")]
    public ToolCall ToolCall { get; set; } = new ToolCall();
}
