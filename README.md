# Anthropic.NET

![Tinonetic.Anthropic Logo](assets/logo.png)

[![anthropic_net NuGet Package](https://img.shields.io/nuget/v/anthropic.net.svg)](https://www.nuget.org/packages/anthropic.net/) [![anthropic_net NuGet Package Downloads](https://img.shields.io/nuget/dt/anthropic.net)](https://www.nuget.org/packages/anthropic.net) [![GitHub Actions Status](https://github.com/tinonetic/anthropic.net/workflows/Build/badge.svg?branch=main)](https://github.com/tinonetic/anthropic.net/actions)

Anthropic.NET is a community-written .NET SDK that gives you access to Anthropic's safety-first language model APIs (Claude).

## Features

Kept in sync with the [Claude API](https://platform.claude.com/docs/en/api/overview) and the official [Python SDK](https://github.com/anthropics/anthropic-sdk-python).

| Area | Support |
| --- | --- |
| Messages API | `MessageAsync`, `StreamMessageAsync` (SSE), `CountTokensAsync` |
| Models | Current catalog in `AnthropicModels` (Fable 5.1, Opus 5.5, Sonnet 5.5, Haiku 4.5, ...), live discovery with `ListModelsAsync` / `GetModelAsync` (context window, capabilities) |
| Thinking and effort | `ThinkingConfig` (adaptive / enabled / between_tools, `display`), `OutputConfig.Effort` (low..max), `ThinkingContentBlock` / `RedactedThinkingContentBlock` |
| Structured outputs | `OutputConfig.Format = OutputFormat.JsonSchema(...)`, `Tool.Strict` |
| Tools | User tools, `ToolChoice`, fine-grained streaming (`EagerInputStreaming`), tool search (`DeferLoading`), programmatic calling (`AllowedCallers`), `RunToolsAsync` auto-loop |
| Server tools | Web search, web fetch, code execution, memory, bash, text editor, tool search, computer toolset, MCP connector (`Tool.WebSearch()` ...) with typed result blocks |
| Content | Text, image (base64 / URL / file), document (PDF / text / URL / file, citations), tool results with rich content, unknown block types preserved |
| Prompt caching | `CacheControl` on system / text / image / document / tool blocks and top-level; cache usage in `Usage` |
| Streaming | All delta types (text, input_json, thinking, signature, citations), `TextAsync()`, `GetFinalMessageAsync()` accumulator |
| Batches | Create / get / list / cancel / delete and streamed JSONL results |
| Files | Upload / list / get / download / delete, `file_id` sources |
| Request options | `metadata`, `service_tier`, `inference_geo`, `speed` (fast mode), `context_management`, `fallbacks`, `container`, `mcp_servers`, per-request `Betas` (`anthropic-beta`) |
| Responses | `stop_reason` incl. `refusal` / `pause_turn`, `StopDetails`, rich `Usage` |
| Reliability | Automatic retries (408/409/429/5xx/529, `retry-after`), typed `AnthropicApiException` (`StatusCode`, `ErrorType`, `RequestId`, `IsRetryable`), timeout, `CancellationToken` everywhere |
| Auth and config | API key or OAuth bearer token, env vars (`ANTHROPIC_API_KEY`, `ANTHROPIC_AUTH_TOKEN`, `ANTHROPIC_BASE_URL`), `AnthropicClientOptions` |
| Managed Agents (beta) | `client.ManagedAgents`: agents, environments, sessions (events incl. SSE stream, threads, resources), scheduled deployments and runs, vaults and credentials, memory stores / memories / versions |
| Skills | `client.Skills`: create, list, get, delete skills and versions |
| Admin API | `client.Admin`: organization, members, invites, workspaces and members, API keys, rate limits, service accounts, federation issuers and rules, CMEK external keys |
| Cloud platforms | `AnthropicBedrockMantle`, `AnthropicAws` (Claude Platform on AWS, SigV4 built in), `AnthropicVertex`, `AnthropicFoundry` |
| DI | `AddAnthropicClient(...)`, resolves `IAnthropicApiClient` |

See [docs/api-coverage.md](docs/api-coverage.md) for the complete endpoint, parameter, content-block and platform map (including details that are implemented but not yet verified against a live account), and [CHANGELOG.md](CHANGELOG.md) for breaking changes when upgrading.

> Newer models reject some parameters (`temperature`/`top_p`/`top_k`, `budget_tokens`, forced `tool_choice`, assistant prefill). See the API docs for per-model rules.

## Installation

Install the package via NuGet:

```bash
dotnet add package Anthropic.Net
```

## Quick Start

### 1. Initialize the Client

You can instantiate `AnthropicApiClient` directly. It manages its own `HttpClient` internally to prevent socket exhaustion.

```csharp
using Anthropic.Net;

var client = new AnthropicApiClient("YOUR_API_KEY");
```

### 2. Chat Completion

```csharp
using Anthropic.Net.Constants;
using Anthropic.Net.Models.Messages;

var messages = new List<Message> { Message.FromUser("Hello, Claude!") };
var request = new MessageRequest(AnthropicModels.ClaudeSonnet55, messages);

var response = await client.MessageAsync(request);
Console.WriteLine(response.Content.OfType<TextContentBlock>().First().Text);
```

### 3. Streaming

```csharp
using Anthropic.Net.Models.Messages.Streaming;

await foreach (var evt in client.StreamMessageAsync(request))
{
    if (evt is ContentBlockDeltaEvent delta && delta.Delta.Type == "text_delta")
    {
        Console.Write(delta.Delta.Text);
    }
}
```

### 4. Tools (Function Calling)

```csharp
var tool = new Tool("get_weather", "Get weather", new
{
    type = "object",
    properties = new { location = new { type = "string" } },
    required = new[] { "location" }
});

var request = new MessageRequest(AnthropicModels.ClaudeSonnet55, messages)
{
    Tools = [tool]
};

var response = await client.MessageAsync(request);

if (response.StopReason == "tool_use")
{
    var toolUse = response.Content.OfType<ToolUseContentBlock>().First();
    Console.WriteLine($"Tool requested: {toolUse.Name}");
}
```

### 5. Vision

```csharp
var imageBlock = await ImageContentBlock.FromFileAsync("path/to/image.png");
var message = new Message("user", new List<ContentBlock> 
{ 
    new TextContentBlock("Describe this image."), 
    imageBlock 
});
```

### 6. Thinking, effort, prompt caching and structured output

```csharp
var request = new MessageRequest(AnthropicModels.ClaudeOpus55, messages, maxTokens: 16000)
{
    System = new List<TextContentBlock> { new("You are terse.") { CacheControl = CacheControl.Ephemeral() } },
    Thinking = ThinkingConfig.Adaptive(display: "summarized"),
    OutputConfig = new OutputConfig { Effort = "high" },
};

var response = await client.MessageAsync(request);
Console.WriteLine($"{response.Text} (cache read: {response.Usage.CacheReadInputTokens})");
```

### 7. Server tools (web search, code execution, ...)

```csharp
var request = new MessageRequest(AnthropicModels.ClaudeOpus55, messages)
{
    Tools = [Tool.WebSearch(maxUses: 3)],
};
var response = await client.MessageAsync(request);
// response.Content contains ServerToolUseContentBlock / WebSearchToolResultContentBlock / cited TextContentBlock
```

### 8. Automatic tool loop and stream accumulation

```csharp
var final = await client.RunToolsAsync(request, async (use, ct) => await MyTools.RunAsync(use.Name, use.Input));

var message = await client.StreamMessageToFinalAsync(request, e => { /* optional per-event callback */ });
await foreach (var text in client.StreamMessageAsync(request).TextAsync()) Console.Write(text);
```

### 9. Token counting, models, batches, files

```csharp
var tokens = await client.CountTokensAsync(request);
var models = await client.ListModelsAsync();
var batch = await client.CreateBatchAsync([new BatchRequestItem("req-1", request)]);
await foreach (var item in client.GetBatchResultsAsync(batch.Id)) { /* match on item.CustomId */ }
var file = await client.UploadFileAsync(File.OpenRead("doc.pdf"), "doc.pdf", "application/pdf");
var doc = DocumentContentBlock.FromFileId(file.Id);
```

### 10. Beta features, retries and errors

```csharp
var client = new AnthropicApiClient(new AnthropicClientOptions { MaxRetries = 3, Timeout = TimeSpan.FromMinutes(5) });
request.Betas = ["compact-2026-01-12"];
try { await client.MessageAsync(request); }
catch (AnthropicApiException ex) when (ex.ErrorType == "rate_limit_error") { Console.WriteLine(ex.RequestId); }
```

### 11. Managed Agents

```csharp
var agent = await client.ManagedAgents.Agents.CreateAsync(new CreateAgentRequest("Researcher", "claude-opus-5-5")
{
    System = "You research topics thoroughly.",
    Tools = [new { type = "agent_toolset_20260401" }],
});
var env = await client.ManagedAgents.Environments.CreateAsync(
    new CreateEnvironmentRequest("sandbox", new { type = "cloud", networking = new { type = "unrestricted" } }));
var session = await client.ManagedAgents.Sessions.CreateAsync(new CreateSessionRequest(agent.Id, env.Id));

// open the stream first, then send the message
await foreach (var e in client.ManagedAgents.Sessions.Events.StreamAsync(session.Id))
{
    if (e.Type == "agent.message") Console.Write(e.Text);
}
await client.ManagedAgents.Sessions.Events.SendAsync(session.Id, [SessionEvents.UserMessage("Summarize the latest on X")]);
```

Agents are created once and referenced by id; archiving an agent, environment or deployment is permanent. Responses keep unknown fields in `AdditionalProperties` (`resource.GetProperty<T>("name")`).

### 12. Admin API

```csharp
using var admin = new AnthropicApiClient(new AnthropicClientOptions { ApiKey = "sk-ant-admin..." });
var users = await admin.Admin.Users.ListAsync(limit: 20);
await admin.Admin.Invites.CreateAsync("dev@example.com", "developer");
await admin.Admin.Workspaces.Members.AddAsync("wrkspc_...", "user_...", "workspace_developer");
```

### 13. Amazon Bedrock, Claude Platform on AWS, Vertex AI, Foundry

```csharp
var bedrock = AnthropicBedrockMantle.Create("us-east-1", apiKey: bedrockApiKey);   // or SigV4 via AWS_* env vars
var request = new MessageRequest(AnthropicBedrockMantle.ModelId("claude-opus-5-5"), messages);

var aws = AnthropicAws.Create(region: "us-east-1", workspaceId: "wrkspc_...");     // bare model ids, full API parity
var vertex = AnthropicVertex.Create("my-project", "global", ct => GetGoogleAccessTokenAsync(ct));
var foundry = AnthropicFoundry.Create(resource: "my-resource", apiKey: foundryKey);
```

Each also has a `Configure(AnthropicClientOptions, ...)` method for use with `AddAnthropicClient(o => ...)`. Third-party platforms support a subset of the API (for example no Batches, Files, Models or Managed Agents on Bedrock and Vertex); see the platform availability table in the Claude docs.

## Dependency Injection

For ASP.NET Core or other DI-based applications, use the extension method to register the client.

```csharp
using Anthropic.Net.Extensions;

// In Program.cs or Startup.cs
builder.Services.AddAnthropicClient("YOUR_API_KEY");
// or: builder.Services.AddAnthropicClient(o => { o.MaxRetries = 4; });  // key from ANTHROPIC_API_KEY
```

You can then inject `IAnthropicApiClient` (or `AnthropicApiClient`) into your controllers or services.

## Demo Application

`Examples/AnthropicNetDemo` is an interactive console app with one demo per feature area. Set your key with `dotnet user-secrets set "Anthropic:ApiKey" "..."` (or `ANTHROPIC_API_KEY`), then:

```bash
cd Examples/AnthropicNetDemo
dotnet run
```

| Demo | Shows |
| --- | --- |
| Chat | multi-turn conversation, `ToAssistantMessage()` |
| Streaming | `StreamMessageToFinalAsync`, usage |
| Tools | strict tools and the automatic `RunToolsAsync` loop |
| Vision | image blocks |
| Thinking + effort | adaptive thinking, streamed reasoning, effort levels |
| Web search | server tool, typed result blocks, sources |
| Structured output | JSON-schema `OutputConfig.Format` |
| Prompt caching | cache write then read, cache token usage |
| Documents with citations | `DocumentContentBlock`, citations |
| Token counting + models | `CountTokensAsync`, `ListModelsAsync` |
| Message Batches | create, poll, stream results |
| Files API | upload, reference by id, list, delete |
| Managed Agents | agent, environment, session, SSE events (confirms before permanent actions) |
| Admin API | read-only organization queries (needs an admin key) |
| Cloud platforms | Bedrock, Claude Platform on AWS, Vertex AI, Foundry |

Most demos spend a few tokens. Typed `AnthropicApiException` details (status, error type, request id) are printed on failure.

## License

MIT
