namespace Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;

public class MessageDeltaEvent : MessageStreamEvent
{
    [JsonPropertyName("delta")]
    public MessageDelta Delta { get; set; } = new();

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = new();
}
