namespace Anthropic.Net.Extensions;

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Models.Messages.Streaming;
using Anthropic.Net.Models.Messages.Streaming.StreamingEvents;

/// <summary>
/// Helpers on top of the raw client: streaming accumulation (like the Python SDK's <c>get_final_message()</c>)
/// and an automatic tool-use loop (like <c>tool_runner</c>).
/// </summary>
public static class AnthropicClientExtensions
{
    /// <summary>Yields only the text deltas of a stream.</summary>
    /// <param name="events">The event stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Text fragments.</returns>
    public static async IAsyncEnumerable<string> TextAsync(this IAsyncEnumerable<MessageStreamEvent> events, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var e in events.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (e is ContentBlockDeltaEvent { Delta: { Type: "text_delta", Text: { } text } })
            {
                yield return text;
            }
        }
    }

    /// <summary>Streams a request and returns the fully accumulated message (text, thinking, tool inputs, citations, usage).</summary>
    /// <param name="client">The client.</param>
    /// <param name="request">The request.</param>
    /// <param name="onEvent">Optional callback invoked for every event (e.g. to print text as it arrives).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The final message.</returns>
    public static Task<MessageResponse> StreamMessageToFinalAsync(this IAnthropicApiClient client, MessageRequest request, Action<MessageStreamEvent>? onEvent = null, CancellationToken cancellationToken = default)
        => client.StreamMessageAsync(request, cancellationToken).GetFinalMessageAsync(onEvent, cancellationToken);

    /// <summary>Accumulates stream events into the final <see cref="MessageResponse"/>.</summary>
    /// <param name="events">The event stream.</param>
    /// <param name="onEvent">Optional callback per event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The final message.</returns>
    /// <exception cref="AnthropicApiException">Thrown on an error event or when no message_start was received.</exception>
    public static async Task<MessageResponse> GetFinalMessageAsync(this IAsyncEnumerable<MessageStreamEvent> events, Action<MessageStreamEvent>? onEvent = null, CancellationToken cancellationToken = default)
    {
        MessageResponse? message = null;
        var json = new Dictionary<int, StringBuilder>();

        await foreach (var e in events.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            onEvent?.Invoke(e);
            switch (e)
            {
                case MessageStartEvent start:
                    message = start.Message;
                    break;
                case ContentBlockStartEvent cbs when message is not null:
                    while (message.Content.Count <= cbs.Index)
                    {
                        message.Content.Add(new ContentBlock());
                    }

                    message.Content[cbs.Index] = cbs.ContentBlock;
                    break;
                case ContentBlockDeltaEvent cbd when message is not null && cbd.Index < message.Content.Count:
                    ApplyDelta(message.Content[cbd.Index], cbd, json);
                    break;
                case ContentBlockStopEvent stop when message is not null && json.Remove(stop.Index, out var sb):
                    SetInput(message.Content[stop.Index], sb.ToString());
                    break;
                case MessageDeltaEvent md when message is not null:
                    message.StopReason = md.Delta.StopReason ?? message.StopReason;
                    message.StopSequence = md.Delta.StopSequence ?? message.StopSequence;
                    message.StopDetails = md.Delta.StopDetails ?? message.StopDetails;
                    message.Usage.OutputTokens = md.Usage.OutputTokens;
                    if (md.Usage.InputTokens > 0)
                    {
                        message.Usage.InputTokens = md.Usage.InputTokens;
                    }

                    message.Usage.CacheReadInputTokens = md.Usage.CacheReadInputTokens ?? message.Usage.CacheReadInputTokens;
                    message.Usage.CacheCreationInputTokens = md.Usage.CacheCreationInputTokens ?? message.Usage.CacheCreationInputTokens;
                    message.Usage.ServerToolUse = md.Usage.ServerToolUse ?? message.Usage.ServerToolUse;
                    break;
                case ErrorEvent err:
                    throw new AnthropicApiException($"Stream error ({err.Error.Type}): {err.Error.Message}", 0, err.Error.Type, null);
            }
        }

        return message ?? throw new AnthropicApiException("The stream ended before a message_start event was received.");
    }

    /// <summary>
    /// Runs the tool-use loop: calls the model, executes every requested client tool with <paramref name="handler"/>
    /// (concurrently, results returned together in one user message), and repeats until the model stops asking for tools.
    /// A thrown handler exception is reported to the model as an <c>is_error</c> tool result. Server-side <c>pause_turn</c> is resumed automatically.
    /// The conversation in <paramref name="request"/>.Messages is extended in place.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="request">The request containing the tools.</param>
    /// <param name="handler">Executes a tool call and returns a string (or content blocks) result.</param>
    /// <param name="maxIterations">Safety limit on model calls.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The final response.</returns>
    public static async Task<MessageResponse> RunToolsAsync(
        this IAnthropicApiClient client,
        MessageRequest request,
        Func<ToolUseContentBlock, CancellationToken, Task<object>> handler,
        int maxIterations = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (request.Messages is not List<Message> && request.Messages is { IsReadOnly: true })
        {
            request.Messages = [.. request.Messages];
        }

        for (var i = 0; ; i++)
        {
            var response = await client.MessageAsync(request, cancellationToken).ConfigureAwait(false);
            if (i + 1 >= maxIterations || (response.StopReason != "tool_use" && response.StopReason != "pause_turn"))
            {
                return response;
            }

            request.Messages.Add(response.ToAssistantMessage());
            if (response.StopReason == "pause_turn")
            {
                continue;
            }

            var results = await Task.WhenAll(response.ToolUses.Select(async use =>
            {
                try
                {
                    return new ToolResultContentBlock(use.Id, await handler(use, cancellationToken).ConfigureAwait(false));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return new ToolResultContentBlock(use.Id, ex.Message) { IsError = true };
                }
            })).ConfigureAwait(false);

            request.Messages.Add(new Message("user", results.Cast<ContentBlock>().ToList()));
        }
    }

    private static void ApplyDelta(ContentBlock block, ContentBlockDeltaEvent e, Dictionary<int, StringBuilder> json)
    {
        var d = e.Delta;
        switch (d.Type)
        {
            case "text_delta" when block is TextContentBlock t:
                t.Text += d.Text;
                break;
            case "thinking_delta" when block is ThinkingContentBlock th:
                th.Thinking += d.Thinking;
                break;
            case "signature_delta" when block is ThinkingContentBlock th:
                th.Signature = d.Signature;
                break;
            case "citations_delta" when block is TextContentBlock t && d.Citation is { } c:
                (t.Citations ??= []).Add(c);
                break;
            case "compaction_delta" when block is CompactionContentBlock cb:
                cb.Content += d.Content;
                break;
            case "input_json_delta":
                if (!json.TryGetValue(e.Index, out var sb))
                {
                    json[e.Index] = sb = new StringBuilder();
                }

                sb.Append(d.PartialJson);
                break;
        }
    }

    private static void SetInput(ContentBlock block, string partialJson)
    {
        if (string.IsNullOrWhiteSpace(partialJson))
        {
            return;
        }

        var input = JsonSerializer.Deserialize<JsonElement>(partialJson);
        switch (block)
        {
            case ToolUseContentBlock tu:
                tu.Input = input;
                break;
            case ServerToolUseContentBlock st:
                st.Input = input;
                break;
            case McpToolUseContentBlock mt:
                mt.Input = input;
                break;
        }
    }
}
