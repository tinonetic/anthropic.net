# Claude API coverage

How Anthropic.NET maps to the [Claude API](https://platform.claude.com/docs/en/api/overview) and the official Python SDK. Last synchronised: October 2026.

Status: **Yes** = implemented and covered by unit tests against mocked HTTP. **Unverified** = implemented from the available reference but not confirmed against a live endpoint (see [Unverified details](#unverified-details)).

## Endpoints

| API | .NET | Status |
| --- | --- | --- |
| `POST /v1/messages` | `MessageAsync` | Yes |
| `POST /v1/messages` (`stream: true`) | `StreamMessageAsync`, `TextAsync`, `GetFinalMessageAsync`, `StreamMessageToFinalAsync` | Yes |
| `POST /v1/messages/count_tokens` | `CountTokensAsync` | Yes |
| `GET /v1/models`, `GET /v1/models/{id}` | `ListModelsAsync`, `GetModelAsync` | Yes |
| `/v1/messages/batches` (+ results, cancel, delete) | `CreateBatchAsync`, `GetBatchAsync`, `ListBatchesAsync`, `CancelBatchAsync`, `DeleteBatchAsync`, `GetBatchResultsAsync` | Yes |
| `/v1/files` (+ content) | `UploadFileAsync`, `ListFilesAsync`, `GetFileAsync`, `DownloadFileAsync`, `DeleteFileAsync` | Yes |
| `/v1/skills` (+ versions) | `client.Skills` | Multipart part names unverified |
| Managed Agents: agents, environments, sessions, events, threads, resources, deployments, deployment runs, vaults, credentials | `client.ManagedAgents.*` | Yes |
| Managed Agents: memory stores, memories, memory versions | `client.ManagedAgents.MemoryStores.*` (`agent-memory-2026-07-22`) | Yes |
| `/v1/organizations/*` (Admin API) | `client.Admin.*` | Beta header and some paths unverified |
| `POST /v1/complete` (legacy) | `CompletionAsync` (obsolete) | Yes |

Not covered: usage and cost reports and the Claude Enterprise user-management and analytics endpoints (the official SDKs do not cover them either), the self-hosted environment worker/poller helpers, webhook signature verification.

## Messages request parameters

| Parameter | .NET |
| --- | --- |
| `model`, `messages`, `max_tokens`, `stop_sequences`, `stream`, `temperature`, `top_p`, `top_k` | `MessageRequest` properties |
| `system` (string or blocks) | `System` |
| `tools`, `tool_choice` | `Tools` (`Tool` + factories), `ToolChoice` |
| `thinking` | `Thinking` (`ThinkingConfig.Adaptive / Enabled / Disabled / BetweenTools`) |
| `output_config` (effort, format, task_budget) | `OutputConfig` |
| `cache_control` | `CacheControl` (top level and on blocks) |
| `metadata`, `service_tier`, `inference_geo`, `speed` | same-named properties |
| `container`, `mcp_servers`, `context_management`, `fallbacks` | same-named properties |
| `anthropic-beta` header | `Betas` (per request) and `AnthropicClientOptions.Betas` (default) |

Per-model rules still apply and are enforced by the API, not the SDK: newer models reject `temperature` / `top_p` / `top_k`, `budget_tokens`, forced `tool_choice` (`any` / `tool`) and assistant prefill, and some models cannot disable thinking. The API returns a 400 `AnthropicApiException` for those.

## Content blocks

| Block | .NET type |
| --- | --- |
| `text` (with citations, cache_control) | `TextContentBlock` |
| `image` (base64, url, file) | `ImageContentBlock` |
| `document` (base64 PDF, text, url, content, file; citations) | `DocumentContentBlock` |
| `tool_use`, `tool_result` | `ToolUseContentBlock`, `ToolResultContentBlock` |
| `thinking`, `redacted_thinking` | `ThinkingContentBlock`, `RedactedThinkingContentBlock` |
| `server_tool_use` | `ServerToolUseContentBlock` |
| `web_search_tool_result`, `web_fetch_tool_result`, `code_execution_tool_result`, `bash_code_execution_tool_result`, `text_editor_code_execution_tool_result`, `tool_search_tool_result` | typed result blocks with a raw `Content` (success arrays and error objects both arrive with HTTP 200) |
| `mcp_tool_use`, `mcp_tool_result` | `McpToolUseContentBlock`, `McpToolResultContentBlock` |
| `container_upload`, `compaction` | `ContainerUploadContentBlock`, `CompactionContentBlock` |
| anything else | `ContentBlock` with the raw fields in `ExtensionData` |

When continuing a conversation, append `response.ToAssistantMessage()` (not just the text) so thinking, tool and compaction blocks are preserved.

## Streaming events

`message_start`, `content_block_start`, `content_block_delta` (`text_delta`, `input_json_delta`, `thinking_delta`, `signature_delta`, `citations_delta`, `compaction_delta`), `content_block_stop`, `message_delta`, `message_stop`, `ping`, `error`. Unknown event types are skipped.

## Server and Anthropic-defined tools

| Tool | Factory |
| --- | --- |
| Web search / web fetch | `Tool.WebSearch()`, `Tool.WebFetch()` (default to the `_20260209` dynamic-filtering types; pass the `type` for older models, e.g. `web_search_20250305`) |
| Code execution | `Tool.CodeExecution()` |
| Memory, bash, text editor | `Tool.Memory()`, `Tool.Bash()`, `Tool.TextEditor()` (client-executed) |
| Tool search | `Tool.ToolSearchRegex()`, `Tool.ToolSearchBm25()` with `DeferLoading` on other tools |
| Computer use | `Tool.ComputerToolset()` |
| MCP connector | `McpServers` + `Tool.McpToolset(name)` with beta `mcp-client-2025-11-20` |

Tool type strings are versioned by the API; any other tool type can be created by constructing a `Tool` and setting `Type`.

## Reliability and configuration

| Feature | .NET |
| --- | --- |
| Retries (408, 409, 429, 5xx incl. 529, connection errors; exponential backoff, `retry-after`) | `AnthropicClientOptions.MaxRetries` (default 2) |
| Timeout | `AnthropicClientOptions.Timeout` |
| Typed errors | `AnthropicApiException`: `StatusCode`, `ErrorType`, `RequestId`, `IsRetryable` |
| API key or bearer token | `ApiKey` / `AuthToken` (an API key wins when both are set, because the API rejects both) |
| Environment variables | `ANTHROPIC_API_KEY`, `ANTHROPIC_AUTH_TOKEN`, `ANTHROPIC_BASE_URL` |
| Cancellation | `CancellationToken` on every async method |
| Request hook | `AnthropicClientOptions.RequestInterceptor` (used by the platform clients) |

Not implemented: Workload Identity Federation token exchange (`ANTHROPIC_FEDERATION_*`), `ant auth` profile files. Obtain a token yourself and pass it as `AuthToken`.

## Cloud platforms

| Platform | Factory | Endpoint | Auth | Models |
| --- | --- | --- | --- | --- |
| Amazon Bedrock | `AnthropicBedrockMantle` | `https://bedrock-mantle.{region}.api.aws/anthropic` | Bedrock API key (bearer) or SigV4 | `anthropic.` prefix (`ModelId()`) |
| Claude Platform on AWS | `AnthropicAws` | `https://aws-external-anthropic.{region}.api.aws` | SigV4 (service `aws-external-anthropic`) or short-term key | bare ids |
| Google Vertex AI | `AnthropicVertex` | `https://{region}-aiplatform.googleapis.com/.../publishers/anthropic/models/{model}:rawPredict` | OAuth access token | bare ids (`@date` for snapshots) |
| Microsoft Foundry | `AnthropicFoundry` | `https://{resource}.services.ai.azure.com/anthropic` | API key or Entra token | bare ids |

Feature availability differs per platform (for example Batches, Files, Models and Managed Agents are first-party and Claude Platform on AWS only; Vertex supports Messages, streaming and token counting). The SDK does not hide unsupported calls except on Vertex, where they throw `NotSupportedException`; elsewhere the platform returns an error.

## Unverified details

These were implemented from the reference material available when this release was written. Check each against a live account before relying on it:

1. **Admin API beta header.** The reference describes the endpoints under `client.beta.organization` without naming a header; none is sent by default. If the API requires one, set `AnthropicClientOptions.AdminBetas`.
2. **Admin API paths** for update, archive and key validation follow Anthropic's usual conventions (`POST /{id}`, `POST /{id}/archive`, `POST /{id}/validate`); the base paths are documented.
3. **Claude Platform on AWS workspace header.** Requests must be routed to a workspace; the header name is a parameter of `AnthropicAws.Configure` (default `anthropic-workspace-id`).
4. **Bedrock Mantle SigV4 service name** (`bedrock-mantle`) is configurable via `AnthropicBedrockMantle.Configure(signingService: ...)`.
5. **Skills create** sends multipart parts `display_title` and `files[]`.
6. **Vertex token counting** uses `models/count-tokens:rawPredict`.
7. **Session event type names** such as `session.status_idle` are matched loosely in the demo; check the events guide for the exact lifecycle.

If you find a mismatch, please open an issue with the request id from `AnthropicApiException.RequestId`.
