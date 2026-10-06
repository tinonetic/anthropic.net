namespace Anthropic.Net;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Serializer options matching the API wire format (null values omitted). Use when serializing requests yourself.</summary>
public static class AnthropicJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}
