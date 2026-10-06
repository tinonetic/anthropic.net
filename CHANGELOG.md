# Changelog

All notable changes to Anthropic.NET. Versions come from git tags (MinVer); this release contains breaking changes, so tag it as a new major version.

## Unreleased (3.0.0)

Synchronised with the Claude API and the official Python SDK as of October 2026. See [docs/api-coverage.md](docs/api-coverage.md) for the complete feature map.

### Added

**Messages API**
- Model constants for the current catalog (Fable 5.1 / 5, Opus 5.5 / 5 / 4.8 / 4.7 / 4.6, Sonnet 5.5 / 5 / 4.6, Haiku 4.5, Opus 4.5, Sonnet 4.5).
- Request parameters: `Thinking` (`ThinkingConfig`), `OutputConfig` (effort, JSON-schema structured outputs, task budget), `Metadata`, `ServiceTier`, `InferenceGeo`, `Speed`, top-level `CacheControl`, `Container`, `McpServers`, `ContextManagement`, `Fallbacks`, per-request `Betas` (sent as `anthropic-beta`).
- `System` may be a string or a list of text blocks (for `cache_control`).
- Typed `ToolChoice`; `Tool` gained `Strict`, `EagerInputStreaming`, `DeferLoading`, `AllowedCallers`, `CacheControl`, and factories for server and Anthropic-defined tools (`WebSearch`, `WebFetch`, `CodeExecution`, `Memory`, `Bash`, `TextEditor`, `ToolSearchRegex`, `ToolSearchBm25`, `ComputerToolset`, `McpToolset`).
- Content blocks: thinking, redacted thinking, server tool use, web search / web fetch / code execution / bash / text editor / tool search results, MCP tool use and result, container upload, compaction, documents (base64, text, URL, file) with citations. Images can use URL and Files API sources.
- Unknown future block types are preserved instead of failing deserialization (`ContentBlockConverter`).
- `Usage`: cache read/write tokens, service tier, inference geo, speed, server tool usage. `MessageResponse`: `StopDetails`, `Container`, `ToolUses`, `ToAssistantMessage()`.
- Streaming: all delta types (text, input_json, thinking, signature, citations, compaction); unknown events are skipped.
- Helpers: `TextAsync()`, `GetFinalMessageAsync()`, `StreamMessageToFinalAsync()`, `RunToolsAsync()` (automatic tool loop, parallel execution, `pause_turn` resume).

**Other endpoints**
- `CountTokensAsync`, `ListModelsAsync` / `GetModelAsync`.
- Message Batches: create, get, list, cancel, delete, streamed results.
- Files API: upload, list, get, download, delete.
- Managed Agents (`client.ManagedAgents`): agents, environments, sessions (events, SSE stream, threads, resources), scheduled deployments and runs, vaults and credentials, memory stores, memories and versions.
- Skills API (`client.Skills`).
- Admin API (`client.Admin`): organization, users, invites, workspaces and members, API keys, rate limits, service accounts, federation, CMEK external keys.
- Cloud platforms: `AnthropicBedrockMantle`, `AnthropicAws`, `AnthropicVertex`, `AnthropicFoundry`, with a dependency-free AWS SigV4 signer (`AwsSigV4`).

**Client**
- `AnthropicClientOptions` (API key or bearer token, base URL, API version, retries, timeout, default betas, admin betas, request interceptor); defaults read `ANTHROPIC_API_KEY`, `ANTHROPIC_AUTH_TOKEN`, `ANTHROPIC_BASE_URL`.
- Automatic retries for 408 / 409 / 429 / 5xx (including 529) and connection errors, honouring `retry-after`.
- `AnthropicApiException` exposes `StatusCode`, `ErrorType`, `RequestId`, `IsRetryable`.
- `CancellationToken` parameters on every async method; `AnthropicJson.Options` exposes the wire serializer options.
- DI: `AddAnthropicClient(Action<AnthropicClientOptions>)`; `IAnthropicApiClient` is now resolvable.

**Demo app** now has 15 demos covering the above (thinking, web search, structured output, caching, citations, models and tokens, batches, files, Managed Agents, Admin, cloud platforms).

### Changed (breaking)
- `IAnthropicApiClient` gained members (new endpoints and the `ManagedAgents`, `Skills`, `Admin` properties). Custom implementations and mocks must be updated.
- `MessageRequest.System` is `object?` (string or text blocks) instead of `string?`.
- `ToolResultContentBlock.Content` is `object` (string or blocks) instead of `string`. The constructor accepts `object`.
- `ImageSource.MediaType` and `Data` are nullable (URL and file sources have neither).
- `ContentBlock` is no longer abstract and `ContentBlock.Type` is not serialized by itself; the converter writes the `type` discriminator.
- `MessageStreamEvent.Type` is set by the client and is not part of the JSON model.
- `MessageRequest.ResponseFormat` and `ResponseFormat` are obsolete and no longer sent (not a Messages API parameter); use `OutputConfig.Format`.
- `CompletionAsync` (Text Completions) is obsolete.
- Retired models are marked `[Obsolete]`: Claude 3 Haiku / Sonnet / Opus, Claude 3.5 Sonnet, Claude v1 and Instant.
- Requests no longer send `null` properties.

### Fixed
- Null fields such as `"system": null` were sent to the API.
- Content blocks could serialize a duplicate `type` property.
- `MessageResponse.Text` threw on non-text blocks.
- Error responses lost their status and error type.
