namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;

/// <summary>
/// Represents the event that marks the start of a message in a streaming message sequence.
/// </summary>
public class MessageStartEvent : MessageStreamEvent
{
    /// <summary>
    /// Gets or sets the message response associated with the start event.
    /// </summary>
    [JsonPropertyName("message")]
    public MessageResponse Message { get; set; } = new();
}
