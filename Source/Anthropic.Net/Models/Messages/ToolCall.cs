namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool call in a message.
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Gets or sets the ID of the tool call.
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
