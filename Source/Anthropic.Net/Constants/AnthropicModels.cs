namespace Anthropic.Net.Constants;

/// <summary>
/// Constants that represent Anthropic Models.
/// </summary>
public static class AnthropicModels
{
    /// <summary>
    /// The largest Anthropic model available. Ideal for a wide range of more complex tasks.
    /// </summary>
    public const string Claude_v1 = "claude-v1";

    /// <summary>
    /// An earlier version of Claude V1.
    /// </summary>
    public const string Claude_v1_0 = "claude-v1.0";

    /// <summary>
    /// An improved version of Claude V1.2. It is slightly better at general helpfulness, instruction following, coding, and other tasks. It is also considerably better with non-English languages. This model also has the ability to role play (in harmless ways) more consistently, and it defaults to writing somewhat longer and more thorough responses.
    /// </summary>
    public const string Claude_v1_2 = "claude-v1.2";

    /// <summary>
    /// A significantly improved version of Claude V1.3. Compared to Claude V1.2, it's more robust against red-team inputs, better at precise instruction-following, better at code, and better and non-English dialogue and writing.
    /// </summary>
    public const string Claude_v1_3 = "claude-v1.3";

    /// <summary>
    /// A smaller model with far lower latency, sampling at roughly 40 words/sec! Its output quality is somewhat lower than Claude V1 models, particularly for complex tasks. However, it is much less expensive and blazing fast. We believe that this model provides more than adequate performance on a range of tasks including text classification, summarization, and lightweight chat applications, as well as search result summarization.
    /// </summary>
    public const string ClaudeInstant_v1 = "claude-instant-v1";

    /// <summary>
    /// The current default for Claude Instant V1.
    /// </summary>
    public const string ClaudeInstant_v1_0 = "claude-instant-v1.0";
}
