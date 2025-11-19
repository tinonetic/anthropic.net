namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages.Streaming;

/// <summary>
/// Represents an error event in a message stream.
/// </summary>
public class ErrorEvent : MessageStreamEvent
{
    /// <summary>
    /// Gets or sets the error details associated with this event.
    /// </summary>
    [JsonPropertyName("error")]
    public StreamingError Error { get; set; } = new();
}
