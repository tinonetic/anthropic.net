namespace Anthropic.Net.Test;

using System.Net;
using System.Text.Json;
using Anthropic.Net.ManagedAgents;
using Anthropic.Net.Models.Messages;
using Anthropic.Net.Platforms;
using Shouldly;
using Xunit;

public class PlatformAndBetaApiTests
{
    private const string Ok = """{"id":"msg_1","type":"message","role":"assistant","content":[{"type":"text","text":"hi"}],"model":"m","stop_reason":"end_turn","usage":{"input_tokens":1,"output_tokens":2}}""";

    private sealed class Recorder : HttpMessageHandler
    {
        private readonly string _body;

        public Recorder(string body) => _body = body;

        public List<(HttpRequestMessage Request, string? Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request, request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body) };
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private static (AnthropicApiClient Client, Recorder Handler) Make(Action<AnthropicClientOptions> configure, string body = Ok)
    {
        var handler = new Recorder(body);
        var options = new AnthropicClientOptions { ApiKey = null, AuthToken = null, MaxRetries = 0 };
        configure(options);
        return (new AnthropicApiClient(options, new Factory(handler)), handler);
    }

    private static MessageRequest Request(string model = "claude-opus-5-5") => new(model, [Message.FromUser("hi")]);

    [Fact]
    public async Task SigV4_MatchesAwsPublishedTestVector()
    {
        // AWS "get-vanilla" signature test vector.
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.amazonaws.com/");
        var creds = new AwsCredentials("AKIDEXAMPLE", "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY");

        await AwsSigV4.SignAsync(request, creds, "us-east-1", "service", new DateTimeOffset(2015, 8, 30, 12, 36, 0, TimeSpan.Zero));

        var auth = request.Headers.GetValues("Authorization").Single();
        auth.ShouldBe("AWS4-HMAC-SHA256 Credential=AKIDEXAMPLE/20150830/us-east-1/service/aws4_request, SignedHeaders=host;x-amz-date, Signature=5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31");
    }

    [Fact]
    public async Task Bedrock_ApiKey_UsesBearerAndMantleUrl()
    {
        var (client, handler) = Make(o => AnthropicBedrockMantle.Configure(o, "us-east-1", apiKey: "bk"));

        await client.MessageAsync(Request(AnthropicBedrockMantle.ModelId("claude-opus-5-5")));

        var req = handler.Calls[0].Request;
        req.RequestUri!.ToString().ShouldBe("https://bedrock-mantle.us-east-1.api.aws/anthropic/v1/messages");
        req.Headers.Authorization!.Parameter.ShouldBe("bk");
        req.Headers.Contains("x-api-key").ShouldBeFalse();
        req.Headers.Contains("anthropic-beta").ShouldBeFalse();
        handler.Calls[0].Body!.ShouldContain("anthropic.claude-opus-5-5");
    }

    [Fact]
    public async Task Bedrock_WithoutApiKey_SignsWithSigV4()
    {
        var creds = new AwsCredentials("AK", "SK", "TOKEN");
        var (client, handler) = Make(o => AnthropicBedrockMantle.Configure(o, "eu-west-1", credentials: creds));

        await client.MessageAsync(Request());

        var req = handler.Calls[0].Request;
        var auth = req.Headers.GetValues("Authorization").Single();
        auth.ShouldStartWith("AWS4-HMAC-SHA256 Credential=AK/");
        auth.ShouldContain("/eu-west-1/bedrock-mantle/aws4_request");
        auth.ShouldContain("x-amz-security-token");
        req.Headers.GetValues("x-amz-security-token").Single().ShouldBe("TOKEN");
    }

    [Fact]
    public async Task AwsPlatform_UsesHostWorkspaceAndSigning()
    {
        var (client, handler) = Make(o => AnthropicAws.Configure(o, "us-west-2", "wrkspc_1", new AwsCredentials("AK", "SK")));

        await client.MessageAsync(Request());

        var req = handler.Calls[0].Request;
        req.RequestUri!.Host.ShouldBe("aws-external-anthropic.us-west-2.api.aws");
        req.Headers.GetValues("anthropic-workspace-id").Single().ShouldBe("wrkspc_1");
        req.Headers.GetValues("Authorization").Single().ShouldContain("/aws-external-anthropic/aws4_request");
        Should.Throw<ArgumentException>(() => AnthropicAws.Configure(new AnthropicClientOptions(), "us-west-2", workspaceId: ""));
    }

    [Fact]
    public async Task Vertex_RewritesUrlAndBody()
    {
        var (client, handler) = Make(o => AnthropicVertex.Configure(o, "proj", "global", _ => Task.FromResult("gcp-token")));
        var request = Request("claude-opus-5-5");
        request.Betas = ["some-beta"];

        await client.MessageAsync(request);

        var call = handler.Calls[0];
        call.Request.RequestUri!.ToString().ShouldBe("https://aiplatform.googleapis.com/v1/projects/proj/locations/global/publishers/anthropic/models/claude-opus-5-5:rawPredict");
        call.Request.Headers.Authorization!.Parameter.ShouldBe("gcp-token");
        call.Request.Headers.Contains("anthropic-version").ShouldBeFalse();
        call.Request.Headers.GetValues("anthropic-beta").Single().ShouldBe("some-beta");
        using var body = JsonDocument.Parse(call.Body!);
        body.RootElement.TryGetProperty("model", out _).ShouldBeFalse();
        body.RootElement.GetProperty("anthropic_version").GetString().ShouldBe("vertex-2023-10-16");
    }

    [Fact]
    public async Task Vertex_StreamCountAndUnsupported()
    {
        var sse = "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";
        var (client, handler) = Make(o => AnthropicVertex.Configure(o, "p", "us-east5", _ => Task.FromResult("t")), sse);

        await foreach (var unused in client.StreamMessageAsync(Request()))
        {
        }

        handler.Calls[0].Request.RequestUri!.ToString().ShouldBe("https://us-east5-aiplatform.googleapis.com/v1/projects/p/locations/us-east5/publishers/anthropic/models/claude-opus-5-5:streamRawPredict");

        var (client2, handler2) = Make(o => AnthropicVertex.Configure(o, "p", "global", _ => Task.FromResult("t")), """{"input_tokens":5}""");
        (await client2.CountTokensAsync(Request())).InputTokens.ShouldBe(5);
        handler2.Calls[0].Request.RequestUri!.AbsolutePath.ShouldEndWith("/publishers/anthropic/models/count-tokens:rawPredict");

        await Should.ThrowAsync<NotSupportedException>(() => client2.ListModelsAsync());
    }

    [Fact]
    public async Task Foundry_UsesResourceUrlAndApiKey()
    {
        var (client, handler) = Make(o => AnthropicFoundry.Configure(o, "myres", "fk"));

        await client.MessageAsync(Request());

        var req = handler.Calls[0].Request;
        req.RequestUri!.ToString().ShouldBe("https://myres.services.ai.azure.com/anthropic/v1/messages");
        req.Headers.GetValues("x-api-key").Single().ShouldBe("fk");

        var (client2, handler2) = Make(o => AnthropicFoundry.Configure(o, "myres", accessTokenProvider: _ => Task.FromResult("entra")));
        await client2.MessageAsync(Request());
        handler2.Calls[0].Request.Headers.Authorization!.Parameter.ShouldBe("entra");
    }

    [Fact]
    public async Task ManagedAgents_AgentsAndSessions_UseBetaHeaderAndPaths()
    {
        var (client, handler) = Make(o => o.ApiKey = "k", """{"id":"agent_1","type":"agent","name":"A","version":1,"future_field":{"x":1}}""");

        var agent = await client.ManagedAgents.Agents.CreateAsync(new CreateAgentRequest("A", "claude-opus-5-5") { System = "be nice" });

        var call = handler.Calls[0];
        call.Request.Method.ShouldBe(HttpMethod.Post);
        call.Request.RequestUri!.AbsolutePath.ShouldBe("/v1/agents");
        call.Request.Headers.GetValues("anthropic-beta").Single().ShouldBe("managed-agents-2026-04-01");
        call.Body!.ShouldContain("\"model\":\"claude-opus-5-5\"");
        agent.Id.ShouldBe("agent_1");
        agent.Version.ShouldBe(1);
        agent.GetProperty<JsonElement>("future_field").GetProperty("x").GetInt32().ShouldBe(1);

        await client.ManagedAgents.Sessions.CreateAsync(new CreateSessionRequest("agent_1", "env_1") { InitialEvents = [SessionEvents.UserMessage("go")] });
        handler.Calls[1].RequestUriPath().ShouldBe("/v1/sessions");
        handler.Calls[1].Body!.ShouldContain("\"type\":\"user.message\"");

        await client.ManagedAgents.Sessions.ListAsync(limit: 5, order: "desc");
        handler.Calls[2].Request.RequestUri!.Query.ShouldBe("?limit=5&order=desc");

        await client.ManagedAgents.Deployments.PauseAsync("depl_1");
        handler.Calls[3].RequestUriPath().ShouldBe("/v1/deployments/depl_1/pause");
    }

    [Fact]
    public async Task ManagedAgents_MemoryStores_UseMemoryBetaOnly()
    {
        var (client, handler) = Make(o => o.ApiKey = "k", """{"id":"mem_1","path":"/a"}""");

        await client.ManagedAgents.MemoryStores.Memories.CreateAsync("memstore_1", "/notes.md", "hello");
        await client.ManagedAgents.MemoryStores.Memories.UpdateAsync("memstore_1", "mem_1", new { content = "x" });

        handler.Calls[0].RequestUriPath().ShouldBe("/v1/memory_stores/memstore_1/memories");
        handler.Calls[0].Request.Headers.GetValues("anthropic-beta").Single().ShouldBe("agent-memory-2026-07-22");
        handler.Calls[1].Request.Method.ShouldBe(HttpMethod.Patch);
    }

    [Fact]
    public async Task ManagedAgents_SessionEvents_SendAndStream()
    {
        var sse = "data: {\"type\":\"agent.message\",\"id\":\"e1\",\"content\":[{\"type\":\"text\",\"text\":\"hel\"},{\"type\":\"text\",\"text\":\"lo\"}]}\n\n"
            + "data: {\"type\":\"session.status_idle\"}\n\n";
        var (client, handler) = Make(o => o.ApiKey = "k", sse);

        var events = new List<SessionEvent>();
        await foreach (var e in client.ManagedAgents.Sessions.Events.StreamAsync("sesn_1", ["agent.message"]))
        {
            events.Add(e);
        }

        events.Count.ShouldBe(2);
        events[0].Text.ShouldBe("hello");
        events[1].Type.ShouldBe("session.status_idle");
        handler.Calls[0].Request.RequestUri!.PathAndQuery.ShouldBe("/v1/sessions/sesn_1/events/stream?event_deltas[]=agent.message");
        handler.Calls[0].Request.Headers.Accept.Single().MediaType.ShouldBe("text/event-stream");

        var (sender, sendHandler) = Make(o => o.ApiKey = "k", """{"events":[]}""");
        await sender.ManagedAgents.Sessions.Events.SendAsync("sesn_1", [SessionEvents.UserMessage("hi"), SessionEvents.Interrupt()]);
        sendHandler.Calls[0].Body!.ShouldContain("\"events\":[");
        sendHandler.Calls[0].RequestUriPath().ShouldBe("/v1/sessions/sesn_1/events");
    }

    [Fact]
    public async Task Admin_UsesOrganizationPaths_AndConfiguredBetas()
    {
        var (client, handler) = Make(o =>
        {
            o.ApiKey = "sk-ant-admin-x";
            o.AdminBetas = ["admin-beta"];
        }, """{"data":[{"id":"user_1","email":"a@b.c","role":"developer"}],"has_more":false}""");

        var page = await client.Admin.Users.ListAsync(limit: 10, email: "a@b.c");
        await client.Admin.Users.UpdateAsync("user_1", "admin");
        await client.Admin.Workspaces.Members.AddAsync("wrkspc_1", "user_1", "workspace_developer");
        await client.Admin.ApiKeys.UpdateAsync("apikey_1", status: "inactive");

        page.Data[0].Email.ShouldBe("a@b.c");
        handler.Calls[0].Request.RequestUri!.PathAndQuery.ShouldBe("/v1/organizations/users?email=a%40b.c&limit=10");
        handler.Calls[0].Request.Headers.GetValues("anthropic-beta").Single().ShouldBe("admin-beta");
        handler.Calls[1].RequestUriPath().ShouldBe("/v1/organizations/users/user_1");
        handler.Calls[2].RequestUriPath().ShouldBe("/v1/organizations/workspaces/wrkspc_1/members");
        handler.Calls[2].Body!.ShouldContain("workspace_developer");
        handler.Calls[3].RequestUriPath().ShouldBe("/v1/organizations/api_keys/apikey_1");
    }

    [Fact]
    public async Task Skills_CreateSendsMultipart()
    {
        var (client, handler) = Make(o => o.ApiKey = "k", """{"id":"skill_1","display_title":"T"}""");

        var skill = await client.Skills.CreateAsync("T", [("my-skill/SKILL.md", "# hi"u8.ToArray())]);

        skill.Id.ShouldBe("skill_1");
        handler.Calls[0].Request.Content!.Headers.ContentType!.MediaType.ShouldBe("multipart/form-data");
        handler.Calls[0].Body!.ShouldContain("display_title");
        handler.Calls[0].Request.Headers.Contains("anthropic-beta").ShouldBeFalse();
    }
}

internal static class RecordedCallExtensions
{
    internal static string RequestUriPath(this (HttpRequestMessage Request, string? Body) call) => call.Request.RequestUri!.AbsolutePath;
}
