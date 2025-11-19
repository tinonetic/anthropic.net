namespace Anthropic.Net.Models.Messages.Streaming;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

/// <summary>
/// Represents a base class for all streaming events.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MessageStartEvent), typeDiscriminator: "message_start")]
[JsonDerivedType(typeof(ContentBlockStartEvent), typeDiscriminator: "content_block_start")]
[JsonDerivedType(typeof(ContentBlockDeltaEvent), typeDiscriminator: "content_block_delta")]
[JsonDerivedType(typeof(ContentBlockStopEvent), typeDiscriminator: "content_block_stop")]
[JsonDerivedType(typeof(MessageDeltaEvent), typeDiscriminator: "message_delta")]
[JsonDerivedType(typeof(MessageStopEvent), typeDiscriminator: "message_stop")]
[JsonDerivedType(typeof(PingEvent), typeDiscriminator: "ping")]
[JsonDerivedType(typeof(ErrorEvent), typeDiscriminator: "error")]
public abstract class MessageStreamEvent
{
    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
