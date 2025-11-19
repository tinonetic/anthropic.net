namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages.Streaming;

/// <summary>
/// Represents an event indicating the end of a content block in a message stream.
/// </summary>
public class ContentBlockStopEvent : MessageStreamEvent
{
    /// <summary>
    /// Gets or sets the index of the content block that has stopped.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }
}
