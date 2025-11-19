namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an error event in the streaming message process.
/// </summary>
public class StreamingError
{
    /// <summary>
    /// Gets or sets the type of the error.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
