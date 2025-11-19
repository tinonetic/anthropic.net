namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;

/// <summary>
/// Represents the start event for a content block in a message stream.
/// </summary>
public class ContentBlockStartEvent : MessageStreamEvent
{
    /// <summary>
    /// Gets or sets the index of the content block in the stream.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the content block associated with this event.
    /// </summary>
    [JsonPropertyName("content_block")]
    public ContentBlock ContentBlock { get; set; } = default!;
}
