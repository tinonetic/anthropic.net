namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages.Streaming;

public class ContentBlockDeltaEvent : MessageStreamEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public ContentBlockDelta Delta { get; set; } = new();
}
