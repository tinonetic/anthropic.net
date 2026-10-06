namespace Anthropic.Net.Models.Batches;

using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.Net.Models.Messages;

/// <summary>One request in a Message Batch. Note: <c>params</c> must not enable streaming.</summary>
public class BatchRequestItem
{
    /// <summary>Initializes a new instance of the <see cref="BatchRequestItem"/> class.</summary>
    /// <param name="customId">Your id for matching results (results arrive in any order).</param>
    /// <param name="parameters">The message request.</param>
    public BatchRequestItem(string customId, MessageRequest parameters)
    {
        CustomId = customId;
        Params = parameters;
    }

    /// <summary>Gets or sets the custom id.</summary>
    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; }

    /// <summary>Gets or sets the message parameters.</summary>
    [JsonPropertyName("params")]
    public MessageRequest Params { get; set; }
}

/// <summary>A Message Batch.</summary>
public class MessageBatch
{
    /// <summary>Gets or sets the id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the processing status: in_progress, canceling, ended.</summary>
    [JsonPropertyName("processing_status")]
    public string ProcessingStatus { get; set; } = string.Empty;

    /// <summary>Gets or sets the request counts.</summary>
    [JsonPropertyName("request_counts")]
    public BatchRequestCounts RequestCounts { get; set; } = new();

    /// <summary>Gets or sets when processing ended.</summary>
    [JsonPropertyName("ended_at")]
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Gets or sets when the batch was created.</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the batch expires.</summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets when cancellation was initiated.</summary>
    [JsonPropertyName("cancel_initiated_at")]
    public DateTimeOffset? CancelInitiatedAt { get; set; }

    /// <summary>Gets or sets the URL of the results file.</summary>
    [JsonPropertyName("results_url")]
    public string? ResultsUrl { get; set; }
}

/// <summary>Counts of requests by outcome.</summary>
public class BatchRequestCounts
{
    /// <summary>Gets or sets the processing count.</summary>
    [JsonPropertyName("processing")]
    public int Processing { get; set; }

    /// <summary>Gets or sets the succeeded count.</summary>
    [JsonPropertyName("succeeded")]
    public int Succeeded { get; set; }

    /// <summary>Gets or sets the errored count.</summary>
    [JsonPropertyName("errored")]
    public int Errored { get; set; }

    /// <summary>Gets or sets the canceled count.</summary>
    [JsonPropertyName("canceled")]
    public int Canceled { get; set; }

    /// <summary>Gets or sets the expired count.</summary>
    [JsonPropertyName("expired")]
    public int Expired { get; set; }
}

/// <summary>One line of batch results.</summary>
public class BatchResultItem
{
    /// <summary>Gets or sets the custom id.</summary>
    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; } = string.Empty;

    /// <summary>Gets or sets the result.</summary>
    [JsonPropertyName("result")]
    public BatchResult Result { get; set; } = new();
}

/// <summary>Outcome of a batch request.</summary>
public class BatchResult
{
    /// <summary>Gets or sets the type: succeeded, errored, canceled, expired.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the message (type "succeeded").</summary>
    [JsonPropertyName("message")]
    public MessageResponse? Message { get; set; }

    /// <summary>Gets or sets the error (type "errored").</summary>
    [JsonPropertyName("error")]
    public JsonElement? Error { get; set; }
}
