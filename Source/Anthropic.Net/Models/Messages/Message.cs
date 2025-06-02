namespace Anthropic.Net.Models.Messages;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a message in a conversation with Claude.
/// </summary>
public class Message
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> class.
    /// </summary>
    /// <param name="role">The role of the message sender (user or assistant).</param>
    /// <param name="content">The content of the message.</param>
    public Message(string role, object content)
    {
        Role = role;
        Content = content;
    }

    /// <summary>
    /// Creates a new user message with text content.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    /// <returns>A new user message.</returns>
    public static Message FromUser(string text)
    {
        return new Message("user", new List<ContentBlock> { new TextContentBlock(text) });
    }

    /// <summary>
    /// Creates a new assistant message with text content.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    /// <returns>A new assistant message.</returns>
    public static Message FromAssistant(string text)
    {
        return new Message("assistant", new List<ContentBlock> { new TextContentBlock(text) });
    }

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public object Content { get; set; }
}