namespace Anthropic.Net.Test;

using System.Net;
using System.Text.Json;
using Anthropic.Net.Constants;
using Anthropic.Net.Extensions;
using Anthropic.Net.Models.Batches;
using Anthropic.Net.Models.Messages;
using Shouldly;
using Xunit;

public class FeatureSyncTests
{
    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

        public QueueHandler(params (HttpStatusCode, string)[] responses) => _responses = new(responses);

        public List<(HttpRequestMessage Request, string? Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request, body));
            var (status, text) = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return new HttpResponseMessage(status) { Content = new StringContent(text) };
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private static (AnthropicApiClient Client, QueueHandler Handler) Create(Action<AnthropicClientOptions>? configure = null, params (HttpStatusCode, string)[] responses)
    {
        var handler = new QueueHandler(responses);
        var options = new AnthropicClientOptions { ApiKey = "k", AuthToken = null, MaxRetries = 2 };
        configure?.Invoke(options);
        return (new AnthropicApiClient(options, new Factory(handler)), handler);
    }

    private const string Ok = """{"id":"msg_1","type":"message","role":"assistant","content":[{"type":"text","text":"hi"}],"model":"claude-opus-5-5","stop_reason":"end_turn","usage":{"input_tokens":1,"output_tokens":2,"cache_read_input_tokens":7}}""";

    [Fact]
    public void Request_SerializesNewParameters_AndOmitsNulls()
    {
        var request = new MessageRequest(AnthropicModels.ClaudeOpus55, [Message.FromUser("hi")], 2000)
        {
            System = new List<TextContentBlock> { new("be brief") { CacheControl = CacheControl.Ephemeral("1h") } },
            Thinking = ThinkingConfig.Adaptive("summarized"),
            OutputConfig = new OutputConfig { Effort = "high", Format = OutputFormat.JsonSchema(new { type = "object" }) },
            Tools = [Tool.WebSearch(3), new Tool("t", "d", new { type = "object" }) { Strict = true }],
            ToolChoice = ToolChoice.Auto(true),
        };

        var json = JsonSerializer.Serialize(request, AnthropicJson.Options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("temperature", out _).ShouldBeFalse();
        root.TryGetProperty("stream", out _).ShouldBeFalse();
        root.GetProperty("system")[0].GetProperty("cache_control").GetProperty("ttl").GetString().ShouldBe("1h");
        root.GetProperty("thinking").GetProperty("type").GetString().ShouldBe("adaptive");
        root.GetProperty("output_config").GetProperty("effort").GetString().ShouldBe("high");
        root.GetProperty("output_config").GetProperty("format").GetProperty("type").GetString().ShouldBe("json_schema");
        root.GetProperty("tools")[0].GetProperty("type").GetString().ShouldBe("web_search_20260209");
        root.GetProperty("tools")[0].TryGetProperty("input_schema", out _).ShouldBeFalse();
        root.GetProperty("tools")[1].GetProperty("strict").GetBoolean().ShouldBeTrue();
        root.GetProperty("tool_choice").GetProperty("disable_parallel_tool_use").GetBoolean().ShouldBeTrue();

        // content blocks carry exactly one "type" property
        var msg = root.GetProperty("messages")[0].GetProperty("content")[0];
        msg.EnumerateObject().Count(p => p.Name == "type").ShouldBe(1);
    }

    [Fact]
    public void Response_ParsesNewAndUnknownBlocks()
    {
        const string json = """
        {"id":"m","type":"message","role":"assistant","model":"x","stop_reason":"refusal",
         "stop_details":{"type":"refusal","category":"cyber","explanation":"no"},
         "content":[
          {"type":"thinking","thinking":"hmm","signature":"sig"},
          {"type":"redacted_thinking","data":"abc"},
          {"type":"server_tool_use","id":"s1","name":"web_search","input":{"query":"q"}},
          {"type":"web_search_tool_result","tool_use_id":"s1","content":[{"type":"web_search_result","url":"u"}]},
          {"type":"text","text":"answer","citations":[{"type":"web_search_result_location","cited_text":"c"}]},
          {"type":"brand_new_block","foo":1}
         ],
         "usage":{"input_tokens":1,"output_tokens":2,"cache_creation_input_tokens":3,"cache_read_input_tokens":4,"service_tier":"standard","server_tool_use":{"web_search_requests":1}}}
        """;

        var r = JsonSerializer.Deserialize<MessageResponse>(json, AnthropicJson.Options)!;

        r.Content[0].ShouldBeOfType<ThinkingContentBlock>().Signature.ShouldBe("sig");
        r.Content[1].ShouldBeOfType<RedactedThinkingContentBlock>();
        r.Content[2].ShouldBeOfType<ServerToolUseContentBlock>().Name.ShouldBe("web_search");
        r.Content[3].ShouldBeOfType<WebSearchToolResultContentBlock>().Content.GetArrayLength().ShouldBe(1);
        r.Content[4].ShouldBeOfType<TextContentBlock>().Citations!.Count.ShouldBe(1);
        r.Content[5].ExtensionData!["foo"].GetInt32().ShouldBe(1);
        r.Text.ShouldBe("answer");
        r.StopDetails!.Category.ShouldBe("cyber");
        r.Usage.CacheCreationInputTokens.ShouldBe(3);
        r.Usage.CacheReadInputTokens.ShouldBe(4);
        r.Usage.ServerToolUse!.Value.GetProperty("web_search_requests").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Message_SendsHeaders_IncludingBetas()
    {
        var (client, handler) = Create(o => o.Betas = ["a-beta"], (HttpStatusCode.OK, Ok));
        var request = new MessageRequest("m", [Message.FromUser("x")]) { Betas = ["b-beta"] };

        var response = await client.MessageAsync(request);

        response.Text.ShouldBe("hi");
        var headers = handler.Calls[0].Request.Headers;
        headers.GetValues("x-api-key").Single().ShouldBe("k");
        headers.GetValues("anthropic-version").Single().ShouldBe("2023-06-01");
        headers.GetValues("anthropic-beta").Single().ShouldBe("a-beta,b-beta");
        handler.Calls[0].Body!.ShouldNotContain("b-beta");
    }

    [Fact]
    public async Task AuthToken_UsesBearerAndOAuthBeta()
    {
        var (client, handler) = Create(o => { o.ApiKey = null; o.AuthToken = "tok"; }, (HttpStatusCode.OK, Ok));

        await client.MessageAsync(new MessageRequest("m", [Message.FromUser("x")]));

        var req = handler.Calls[0].Request;
        req.Headers.Authorization!.Parameter.ShouldBe("tok");
        req.Headers.Contains("x-api-key").ShouldBeFalse();
        req.Headers.GetValues("anthropic-beta").Single().ShouldContain("oauth-2025-04-20");
    }

    [Fact]
    public async Task Retries_Overloaded_ThenSucceeds()
    {
        var overloaded = """{"type":"error","error":{"type":"overloaded_error","message":"busy"}}""";
        var (client, handler) = Create(null, ((HttpStatusCode)529, overloaded), (HttpStatusCode.OK, Ok));
        // first response is 529 (queue dequeues when more than one remains)
        var response = await client.MessageAsync(new MessageRequest("m", [Message.FromUser("x")]));

        response.Id.ShouldBe("msg_1");
        handler.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Error_ExposesStatusTypeAndRequestId()
    {
        var body = """{"type":"error","error":{"type":"invalid_request_error","message":"bad"},"request_id":"req_9"}""";
        var (client, _) = Create(o => o.MaxRetries = 0, (HttpStatusCode.BadRequest, body));

        var ex = await Should.ThrowAsync<AnthropicApiException>(() => client.MessageAsync(new MessageRequest("m", [Message.FromUser("x")])));

        ex.StatusCode.ShouldBe(400);
        ex.ErrorType.ShouldBe("invalid_request_error");
        ex.RequestId.ShouldBe("req_9");
        ex.IsRetryable.ShouldBeFalse();
    }

    [Fact]
    public async Task CountTokens_SendsOnlyAllowedFields()
    {
        var (client, handler) = Create(null, (HttpStatusCode.OK, """{"input_tokens":42}"""));
        var request = new MessageRequest("m", [Message.FromUser("x")], 999) { Temperature = 0.5f, Thinking = ThinkingConfig.Adaptive() };

        var count = await client.CountTokensAsync(request);

        count.InputTokens.ShouldBe(42);
        handler.Calls[0].Request.RequestUri!.AbsolutePath.ShouldBe("/v1/messages/count_tokens");
        using var doc = JsonDocument.Parse(handler.Calls[0].Body!);
        doc.RootElement.TryGetProperty("max_tokens", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("temperature", out _).ShouldBeFalse();
        doc.RootElement.TryGetProperty("thinking", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Models_ListAndGet()
    {
        var list = """{"data":[{"id":"claude-opus-5-5","display_name":"Claude Opus 5.5","created_at":"2026-01-01T00:00:00Z","max_input_tokens":1000000,"max_tokens":128000,"capabilities":{"image_input":{"supported":true}}}],"has_more":false,"first_id":"a","last_id":"b"}""";
        var (client, handler) = Create(null, (HttpStatusCode.OK, list));

        var page = await client.ListModelsAsync(limit: 5, afterId: "x");

        page.Data[0].MaxInputTokens.ShouldBe(1_000_000);
        page.Data[0].Capabilities!.Value.GetProperty("image_input").GetProperty("supported").GetBoolean().ShouldBeTrue();
        handler.Calls[0].Request.RequestUri!.Query.ShouldBe("?limit=5&after_id=x");
    }

    [Fact]
    public async Task Batches_CreateAndStreamResults()
    {
        var batch = """{"id":"msgbatch_1","type":"message_batch","processing_status":"in_progress","request_counts":{"processing":1,"succeeded":0,"errored":0,"canceled":0,"expired":0},"created_at":"2026-01-01T00:00:00Z"}""";
        var results = """{"custom_id":"a","result":{"type":"succeeded","message":""" + Ok + """}}""" + "\n"
            + """{"custom_id":"b","result":{"type":"errored","error":{"type":"error"}}}""" + "\n";
        var (client, handler) = Create(null, (HttpStatusCode.OK, batch), (HttpStatusCode.OK, results));

        var created = await client.CreateBatchAsync([new BatchRequestItem("a", new MessageRequest("m", [Message.FromUser("x")]))]);
        created.ProcessingStatus.ShouldBe("in_progress");
        handler.Calls[0].Body!.ShouldContain("\"custom_id\":\"a\"");

        var items = new List<BatchResultItem>();
        await foreach (var item in client.GetBatchResultsAsync("msgbatch_1"))
        {
            items.Add(item);
        }

        items.Count.ShouldBe(2);
        items[0].Result.Message!.Text.ShouldBe("hi");
        items[1].Result.Type.ShouldBe("errored");
    }

    [Fact]
    public async Task Stream_GetFinalMessage_AccumulatesToolInputThinkingAndUsage()
    {
        var sse = string.Join(
            "\n\n",
            """event: message_start""" + "\n" + """data: {"type":"message_start","message":{"id":"m","type":"message","role":"assistant","content":[],"model":"x","usage":{"input_tokens":10,"output_tokens":1}}}""",
            """event: content_block_start""" + "\n" + """data: {"type":"content_block_start","index":0,"content_block":{"type":"thinking","thinking":"","signature":""}}""",
            """event: content_block_delta""" + "\n" + """data: {"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"ab"}}""",
            """event: content_block_delta""" + "\n" + """data: {"type":"content_block_delta","index":0,"delta":{"type":"signature_delta","signature":"S"}}""",
            """event: content_block_start""" + "\n" + """data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"t1","name":"w","input":{}}}""",
            """event: content_block_delta""" + "\n" + """data: {"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":"{\"city\":"}}""",
            """event: content_block_delta""" + "\n" + """data: {"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":"\"Paris\"}"}}""",
            """event: content_block_stop""" + "\n" + """data: {"type":"content_block_stop","index":1}""",
            """event: message_delta""" + "\n" + """data: {"type":"message_delta","delta":{"stop_reason":"tool_use"},"usage":{"output_tokens":9}}""",
            """event: some_future_event""" + "\n" + """data: {"type":"some_future_event"}""",
            """event: message_stop""" + "\n" + """data: {"type":"message_stop"}""") + "\n\n";
        var (client, _) = Create(null, (HttpStatusCode.OK, sse));

        var final = await client.StreamMessageToFinalAsync(new MessageRequest("m", [Message.FromUser("x")]));

        final.Content[0].ShouldBeOfType<ThinkingContentBlock>().Thinking.ShouldBe("ab");
        ((ThinkingContentBlock)final.Content[0]).Signature.ShouldBe("S");
        var tool = final.Content[1].ShouldBeOfType<ToolUseContentBlock>();
        ((JsonElement)tool.Input).GetProperty("city").GetString().ShouldBe("Paris");
        final.StopReason.ShouldBe("tool_use");
        final.Usage.OutputTokens.ShouldBe(9);
        final.Usage.InputTokens.ShouldBe(10);
    }

    [Fact]
    public async Task RunTools_ExecutesToolsAndReturnsFinalAnswer()
    {
        var toolTurn = """{"id":"1","type":"message","role":"assistant","model":"x","stop_reason":"tool_use","content":[{"type":"tool_use","id":"t1","name":"w","input":{"city":"Paris"}},{"type":"tool_use","id":"t2","name":"w","input":{"city":"Rome"}}],"usage":{"input_tokens":1,"output_tokens":1}}""";
        var (client, handler) = Create(null, (HttpStatusCode.OK, toolTurn), (HttpStatusCode.OK, Ok));
        var request = new MessageRequest("m", [Message.FromUser("weather?")]) { Tools = [new Tool("w", "weather", new { type = "object" })] };

        var final = await client.RunToolsAsync(request, (use, _) =>
            Task.FromResult<object>(use.Name == "w" ? "sunny" : throw new InvalidOperationException()));

        final.Text.ShouldBe("hi");
        handler.Calls.Count.ShouldBe(2);
        using var doc = JsonDocument.Parse(handler.Calls[1].Body!);
        var last = doc.RootElement.GetProperty("messages").EnumerateArray().Last();
        last.GetProperty("role").GetString().ShouldBe("user");
        last.GetProperty("content").GetArrayLength().ShouldBe(2); // both results in ONE user message
        last.GetProperty("content")[0].GetProperty("tool_use_id").GetString().ShouldBe("t1");
    }

    [Fact]
    public void Document_And_Image_Sources_Serialize()
    {
        var pdf = DocumentContentBlock.FromPdf([1, 2, 3], citations: true);
        var json = JsonSerializer.Serialize<ContentBlock>(pdf, AnthropicJson.Options);
        json.ShouldContain("\"type\":\"document\"");
        json.ShouldContain("application/pdf");
        json.ShouldContain("\"citations\":{\"enabled\":true}");
        JsonSerializer.Serialize<ContentBlock>(ImageContentBlock.FromUrl("https://x/y.png"), AnthropicJson.Options).ShouldContain("\"url\":\"https://x/y.png\"");
        JsonSerializer.Serialize<ContentBlock>(ImageContentBlock.FromFileId("file_1"), AnthropicJson.Options).ShouldContain("\"file_id\":\"file_1\"");
    }
}
