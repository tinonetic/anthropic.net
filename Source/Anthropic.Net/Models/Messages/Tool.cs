namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool that can be used by Claude.
/// </summary>
public class Tool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Tool"/> class.
    /// </summary>
    /// <param name="name">The name of the tool.</param>
    /// <param name="description">The description of the tool.</param>
    /// <param name="inputSchema">The input schema for the tool.</param>
    public Tool(string name, string description, object inputSchema)
    {
        Name = name;
        Description = description;
        InputSchema = inputSchema;
    }

    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the tool.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the input schema for the tool.
    /// </summary>
    [JsonPropertyName("input_schema")]
    public object InputSchema { get; set; }
}

/// <summary>
/// Represents a tool call in a message.
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Gets or sets the ID of the tool call.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tool.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input to the tool.
    /// </summary>
    [JsonPropertyName("input")]
    public object Input { get; set; } = new();
}

/// <summary>
/// Represents a tool use in a message.
/// </summary>
public class ToolUse
{
    /// <summary>
    /// Gets or sets the type of the content block.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "tool_use";

    /// <summary>
    /// Gets or sets the tool call.
    /// </summary>
    [JsonPropertyName("tool_use")]
    public ToolCall ToolCall { get; set; } = new ToolCall();
}