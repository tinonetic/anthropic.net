namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a delta update to a content block in a streaming message event.
/// </summary>
public class ContentBlockDelta
{
    /// <summary>
    /// Gets or sets the type of the content block delta.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text content of the delta, if any.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
