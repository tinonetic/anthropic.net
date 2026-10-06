namespace Anthropic.Net.Constants;

/// <summary>
/// Constants that represent Anthropic Models. Use <c>ListModelsAsync</c> for live discovery.
/// </summary>
public static class AnthropicModels
{
    /// <summary>Claude Fable 5.1 - most capable widely released model.</summary>
    public const string ClaudeFable51 = "claude-fable-5-1";

    /// <summary>Claude Fable 5.</summary>
    public const string ClaudeFable5 = "claude-fable-5";

    /// <summary>Claude Opus 5.5 - current Opus.</summary>
    public const string ClaudeOpus55 = "claude-opus-5-5";

    /// <summary>Claude Opus 5.</summary>
    public const string ClaudeOpus5 = "claude-opus-5";

    /// <summary>Claude Opus 4.8.</summary>
    public const string ClaudeOpus48 = "claude-opus-4-8";

    /// <summary>Claude Opus 4.7.</summary>
    public const string ClaudeOpus47 = "claude-opus-4-7";

    /// <summary>Claude Opus 4.6.</summary>
    public const string ClaudeOpus46 = "claude-opus-4-6";

    /// <summary>Claude Sonnet 5.5 - current Sonnet.</summary>
    public const string ClaudeSonnet55 = "claude-sonnet-5-5";

    /// <summary>Claude Sonnet 5.</summary>
    public const string ClaudeSonnet5 = "claude-sonnet-5";

    /// <summary>Claude Sonnet 4.6.</summary>
    public const string ClaudeSonnet46 = "claude-sonnet-4-6";

    /// <summary>Claude Haiku 4.5 - fastest and most cost-effective.</summary>
    public const string ClaudeHaiku45 = "claude-haiku-4-5";

    /// <summary>Claude Opus 4.5.</summary>
    public const string ClaudeOpus45 = "claude-opus-4-5";

    /// <summary>Claude Sonnet 4.5.</summary>
    public const string ClaudeSonnet45 = "claude-sonnet-4-5";

    /// <summary>Claude Opus 4.1 (deprecated).</summary>
    [Obsolete("Claude Opus 4.1 is deprecated. Use ClaudeOpus55.")]
    public const string ClaudeOpus41 = "claude-opus-4-1";

    /// <summary>Claude Sonnet 4 (deprecated).</summary>
    [Obsolete("Claude Sonnet 4 is deprecated. Use ClaudeSonnet55.")]
    public const string ClaudeSonnet4 = "claude-sonnet-4-0";

    /// <summary>Claude Opus 4 (deprecated).</summary>
    [Obsolete("Claude Opus 4 is deprecated. Use ClaudeOpus55.")]
    public const string ClaudeOpus4 = "claude-opus-4-0";

    /// <summary>Claude 3 Haiku (retired).</summary>
    [Obsolete("Claude 3 Haiku is retired. Use ClaudeHaiku45.")]
    public const string Claude3Haiku = "claude-3-haiku-20240307";

    /// <summary>Claude 3 Sonnet (retired).</summary>
    [Obsolete("Claude 3 Sonnet is retired. Use ClaudeSonnet55.")]
    public const string Claude3Sonnet = "claude-3-sonnet-20240229";

    /// <summary>Claude 3 Opus (retired).</summary>
    [Obsolete("Claude 3 Opus is retired. Use ClaudeOpus55.")]
    public const string Claude3Opus = "claude-3-opus-20240229";

    /// <summary>Claude 3.5 Sonnet (retired).</summary>
    [Obsolete("Claude 3.5 Sonnet is retired. Use ClaudeSonnet55.")]
    public const string Claude35Sonnet = "claude-3-5-sonnet-20241022";

    /// <summary>Claude v1.</summary>
    [Obsolete("Claude v1 models are retired. Use a current model.")]
    public const string Claude_v1 = "claude-v1";

    /// <summary>Claude v1.0.</summary>
    [Obsolete("Claude v1 models are retired. Use a current model.")]
    public const string Claude_v1_0 = "claude-v1.0";

    /// <summary>Claude v1.2.</summary>
    [Obsolete("Claude v1 models are retired. Use a current model.")]
    public const string Claude_v1_2 = "claude-v1.2";

    /// <summary>Claude v1.3.</summary>
    [Obsolete("Claude v1 models are retired. Use a current model.")]
    public const string Claude_v1_3 = "claude-v1.3";

    /// <summary>Claude Instant v1.</summary>
    [Obsolete("Claude Instant models are retired. Use a current model.")]
    public const string ClaudeInstant_v1 = "claude-instant-v1";

    /// <summary>Claude Instant v1.0.</summary>
    [Obsolete("Claude Instant models are retired. Use a current model.")]
    public const string ClaudeInstant_v1_0 = "claude-instant-v1.0";
}
