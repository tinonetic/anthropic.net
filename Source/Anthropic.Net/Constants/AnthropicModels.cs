namespace Anthropic.Net.Constants;

/// <summary>
/// Constants that represent Anthropic Models.
/// </summary>
public static class AnthropicModels
{
    /// <summary>
    /// The largest Anthropic model available. Ideal for a wide range of more complex tasks.
    /// </summary>
    [Obsolete("Claude v1 models are deprecated. Please use Claude 3 models instead.")]
    public const string Claude_v1 = "claude-v1";

    /// <summary>
    /// An earlier version of Claude V1.
    /// </summary>
    [Obsolete("Claude v1 models are deprecated. Please use Claude 3 models instead.")]
    public const string Claude_v1_0 = "claude-v1.0";

    /// <summary>
    /// An improved version of Claude V1.2.
    /// </summary>
    [Obsolete("Claude v1 models are deprecated. Please use Claude 3 models instead.")]
    public const string Claude_v1_2 = "claude-v1.2";

    /// <summary>
    /// A significantly improved version of Claude V1.3.
    /// </summary>
    [Obsolete("Claude v1 models are deprecated. Please use Claude 3 models instead.")]
    public const string Claude_v1_3 = "claude-v1.3";

    /// <summary>
    /// A smaller model with far lower latency.
    /// </summary>
    [Obsolete("Claude Instant v1 models are deprecated. Please use Claude 3 models instead.")]
    public const string ClaudeInstant_v1 = "claude-instant-v1";

    /// <summary>
    /// The current default for Claude Instant V1.
    /// </summary>
    [Obsolete("Claude Instant v1 models are deprecated. Please use Claude 3 models instead.")]
    public const string ClaudeInstant_v1_0 = "claude-instant-v1.0";

    /// <summary>
    /// Claude 3 Haiku - Fastest and most compact model in the Claude 3 family.
    /// </summary>
    public const string Claude_3_Haiku = "claude-3-haiku-20240307";

    /// <summary>
    /// Claude 3 Sonnet - Balanced model for a wide range of tasks.
    /// </summary>
    public const string Claude_3_Sonnet = "claude-3-sonnet-20240229";

    /// <summary>
    /// Claude 3 Opus - Most powerful model in the Claude 3 family.
    /// </summary>
    public const string Claude_3_Opus = "claude-3-opus-20240229";

    /// <summary>
    /// Claude 3.5 Sonnet - Latest model with improved capabilities.
    /// </summary>
    public const string Claude_3_5_Sonnet = "claude-3-5-sonnet-20241022";
}
