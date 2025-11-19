namespace Anthropic.Net.Models.Messages.Streaming;

using System.Text.Json.Serialization;

public class MessageStartEvent : MessageStreamEvent
{
    [JsonPropertyName("message")]
    public MessageResponse Message { get; set; } = new();
}

public class ContentBlockStartEvent : MessageStreamEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("content_block")]
    public ContentBlock ContentBlock { get; set; } = default!;
}

public class ContentBlockDeltaEvent : MessageStreamEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public ContentBlockDelta Delta { get; set; } = new();
}

public class ContentBlockStopEvent : MessageStreamEvent
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

public class MessageDeltaEvent : MessageStreamEvent
{
    [JsonPropertyName("delta")]
    public MessageDelta Delta { get; set; } = new();

    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = new();
}

public class MessageStopEvent : MessageStreamEvent
{
}

public class PingEvent : MessageStreamEvent
{
}

public class ErrorEvent : MessageStreamEvent
{
    [JsonPropertyName("error")]
    public Error Error { get; set; } = new();
}

public class ContentBlockDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class MessageDelta
{
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}

public class Error
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
