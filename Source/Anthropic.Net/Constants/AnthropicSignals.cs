namespace Anthropic.Net.Constants;

/// <summary>
/// The Human and Assistant Constants used in the Anthropic Prompts.
/// </summary>
public static class AnthropicSignals
{
    /// <summary>
    /// The prefix for a human-generated message in a prompt.
    /// </summary>
    public const string HumanSignal = "\\n\\nHuman: ";

    /// <summary>
    /// The prefix for an AI-generated message in a prompt.
    /// </summary>
    public const string AssistantSignal = "\\n\\nAssistant: ";
}
