namespace Anthropic.Net.Models.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Thinking configuration. Use <see cref="Adaptive"/> on Claude 4.6+ models (default choice);
/// <see cref="Enabled"/> (budget_tokens) only for Haiku 4.5 and older; <see cref="BetweenTools"/> on Sonnet 5.5 to turn thinking off.
/// </summary>
public class ThinkingConfig
{
    /// <summary>Gets or sets the type: adaptive, enabled, disabled, between_tools.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "adaptive";

    /// <summary>Gets or sets the token budget (type "enabled" only; min 1024, &lt; max_tokens).</summary>
    [JsonPropertyName("budget_tokens")]
    public int? BudgetTokens { get; set; }

    /// <summary>Gets or sets the display mode: "summarized", "omitted" or "updates".</summary>
    [JsonPropertyName("display")]
    public string? Display { get; set; }

    /// <summary>Adaptive thinking.</summary>
    /// <param name="display">Optional display mode ("summarized" to stream readable reasoning).</param>
    /// <returns>The config.</returns>
    public static ThinkingConfig Adaptive(string? display = null) => new() { Type = "adaptive", Display = display };

    /// <summary>Fixed-budget thinking (older models).</summary>
    /// <param name="budgetTokens">Budget (&gt;= 1024).</param>
    /// <returns>The config.</returns>
    public static ThinkingConfig Enabled(int budgetTokens) => new() { Type = "enabled", BudgetTokens = budgetTokens };

    /// <summary>Thinking disabled.</summary>
    /// <returns>The config.</returns>
    public static ThinkingConfig Disabled() => new() { Type = "disabled" };

    /// <summary>Lowest thinking setting (Sonnet 5.5).</summary>
    /// <returns>The config.</returns>
    public static ThinkingConfig BetweenTools() => new() { Type = "between_tools" };
}

/// <summary>Output configuration: effort, structured outputs and task budgets.</summary>
public class OutputConfig
{
    /// <summary>Gets or sets the effort: low, medium, high, xhigh, max.</summary>
    [JsonPropertyName("effort")]
    public string? Effort { get; set; }

    /// <summary>Gets or sets the structured-output format.</summary>
    [JsonPropertyName("format")]
    public OutputFormat? Format { get; set; }

    /// <summary>Gets or sets the task budget, e.g. { type = "tokens", total = 64000 } (beta task-budgets-2026-03-13).</summary>
    [JsonPropertyName("task_budget")]
    public object? TaskBudget { get; set; }
}

/// <summary>Structured output format (JSON schema).</summary>
public class OutputFormat
{
    /// <summary>Gets or sets the type; "json_schema".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_schema";

    /// <summary>Gets or sets the JSON schema.</summary>
    [JsonPropertyName("schema")]
    public object? Schema { get; set; }

    /// <summary>Creates a JSON-schema output format.</summary>
    /// <param name="schema">An object (or JsonElement) containing the JSON schema.</param>
    /// <returns>The format.</returns>
    public static OutputFormat JsonSchema(object schema) => new() { Schema = schema };
}

/// <summary>Request metadata.</summary>
public class RequestMetadata
{
    /// <summary>Gets or sets an opaque external user id.</summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
