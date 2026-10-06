namespace Anthropic.Net.Models.Messages;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

/// <summary>
/// Reads/writes <see cref="ContentBlock"/> by its "type" discriminator. Unlike built-in polymorphism it tolerates
/// block types this SDK does not know yet: they are returned as a base <see cref="ContentBlock"/> whose fields are in
/// <see cref="ContentBlock.ExtensionData"/>, and written back unchanged.
/// </summary>
public sealed class ContentBlockConverter : JsonConverter<ContentBlock>
{
    private static readonly Dictionary<string, Type> Known = new()
    {
        ["text"] = typeof(TextContentBlock),
        ["image"] = typeof(ImageContentBlock),
        ["document"] = typeof(DocumentContentBlock),
        ["tool_use"] = typeof(ToolUseContentBlock),
        ["tool_result"] = typeof(ToolResultContentBlock),
        ["thinking"] = typeof(ThinkingContentBlock),
        ["redacted_thinking"] = typeof(RedactedThinkingContentBlock),
        ["server_tool_use"] = typeof(ServerToolUseContentBlock),
        ["web_search_tool_result"] = typeof(WebSearchToolResultContentBlock),
        ["web_fetch_tool_result"] = typeof(WebFetchToolResultContentBlock),
        ["code_execution_tool_result"] = typeof(CodeExecutionToolResultContentBlock),
        ["bash_code_execution_tool_result"] = typeof(BashCodeExecutionToolResultContentBlock),
        ["text_editor_code_execution_tool_result"] = typeof(TextEditorCodeExecutionToolResultContentBlock),
        ["tool_search_tool_result"] = typeof(ToolSearchToolResultContentBlock),
        ["mcp_tool_use"] = typeof(McpToolUseContentBlock),
        ["mcp_tool_result"] = typeof(McpToolResultContentBlock),
        ["container_upload"] = typeof(ContainerUploadContentBlock),
        ["compaction"] = typeof(CompactionContentBlock),
    };

    /// <inheritdoc/>
    public override ContentBlock? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? string.Empty : string.Empty;

        // A concrete type was requested explicitly (e.g. Deserialize<TextContentBlock> never reaches here); otherwise map by discriminator.
        var target = Known.TryGetValue(type, out var known) ? known : typeof(ContentBlock);
        ContentBlock block;
        if (target == typeof(ContentBlock))
        {
            block = new ContentBlock { ExtensionData = [] };
            foreach (var p in root.EnumerateObject())
            {
                block.ExtensionData[p.Name] = p.Value.Clone();
            }
        }
        else
        {
            block = (ContentBlock)root.Deserialize(target, options)!;
        }

        block.Type = type;
        return block;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ContentBlock value, JsonSerializerOptions options)
    {
        var runtimeType = value.GetType();
        var node = runtimeType == typeof(ContentBlock)
            ? JsonSerializer.SerializeToNode(value.ExtensionData ?? [], options) as JsonObject ?? []
            : JsonSerializer.SerializeToNode(value, runtimeType, options) as JsonObject ?? [];

        var ordered = new JsonObject();
        if (!string.IsNullOrEmpty(value.Type))
        {
            ordered["type"] = value.Type;
        }

        foreach (var kv in node.ToList())
        {
            if (kv.Key == "type")
            {
                continue;
            }

            node.Remove(kv.Key);
            ordered[kv.Key] = kv.Value;
        }

        ordered.WriteTo(writer, options);
    }
}
