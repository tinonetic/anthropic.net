namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool use content block in a message.
/// </summary>
public class ToolUseContentBlock : ContentBlock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolUseContentBlock"/> class.
    /// </summary>
    public ToolUseContentBlock()
    {
        Type = "tool_use";
    }

    /// <summary>
    /// Gets or sets the ID of the tool use.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input to the tool.
    /// </summary>
    [JsonPropertyName("input")]
    public object Input { get; set; } = new();
}
