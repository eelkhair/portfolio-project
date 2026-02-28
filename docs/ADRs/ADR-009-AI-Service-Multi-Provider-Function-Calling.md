# ADR-009: AI Service — Microsoft.Extensions.AI with Multi-Provider Function Calling

- **Status:** Accepted
- **Date:** 2026-02-28

## Context

The AI service (v2) provides an LLM-powered assistant that generates job drafts, creates companies, publishes jobs, and answers domain questions. It needs to support **multiple LLM providers** (OpenAI, Azure OpenAI, Gemini, Claude) to avoid vendor lock-in, enable cost comparison, and demonstrate provider-agnostic design.

Historically, integrating multiple LLM providers meant maintaining separate SDK abstractions, duplicating tool definitions per provider, and writing custom function-calling loops. The service also runs in two deployment topologies — **monolith mode** (direct HTTP to the monolith API) and **microservices mode** (via Dapr service invocation to distributed services) — requiring tools to be composable based on runtime configuration.

## Decision

### Provider Abstraction via Microsoft.Extensions.AI

We adopt **Microsoft.Extensions.AI** as the provider abstraction layer rather than building a custom one or coupling to a single SDK. Each provider is registered as a **keyed singleton** `IChatClient` in DI:

| Key | Provider | Adapter |
|-----|----------|---------|
| `openai` | OpenAI SDK | `.AsIChatClient()` |
| `azure` | Azure.AI.OpenAI SDK | `.AsIChatClient()` |
| `gemini` | GeminiDotnet.Extensions.AI | Native `IChatClient` |
| `claude` | Anthropic.SDK | `.Messages` adapter |

Provider selection is **runtime-configurable** via a Redis-backed setting (`AIProvider`), changeable through the Settings API without redeployment. `ChatService.GetClient()` resolves the active provider with `GetRequiredKeyedService<IChatClient>(provider)`.

### Automatic Function Calling with FunctionInvokingChatClient

Each chat request wraps the resolved `IChatClient` in a `FunctionInvokingChatClient`, which handles the tool-call loop automatically — the LLM proposes tool calls, the wrapper invokes them, and results feed back into the conversation until the model produces a final text response.

### Topology-Aware Tool Registries

Tools implement `IAiTools` and are organized into three registries:

1. **AiToolRegistry** (keyed `"ai"`) — AI-domain tools: draft generation, draft listing, draft saving, location lookup, conversation and provider metadata. Always included.
2. **MonolithToolRegistry** (keyed `"monolith"`) — Topology-specific tools for monolith mode: company listing, industry listing, company creation, job creation via direct HTTP.
3. **AdminToolRegistry** (keyed `"micro"`) — Same operations routed through the admin microservice via Dapr service invocation.

`ChatOptionsFactory` selects the topology registry based on a `FeatureFlags:Monolith` feature flag, merges it with the AI registry, and performs **duplicate detection** (`GroupBy` on tool name) to fail fast on naming collisions.

### Conversation State

Conversations are stored in Redis (DB 2) with a **2-day TTL**. A **sliding window** of the last 40 messages is maintained per conversation to bound token usage while preserving context. Each conversation record also stores the last 15 `TraceParent` headers, enabling distributed trace linking across chat turns.

### Tool Result Envelope

All tools return a `ToolResultEnvelope<T>` containing the data, item count, and execution timestamp — giving the LLM structured context about result freshness and size. Read-heavy tools (company list, industry list) use `IMemoryCache` with a 5-minute per-conversation TTL.

## Consequences

### Positive

- **Provider portability**: Switching providers requires only a configuration change, not code changes. Useful for cost optimization, capability comparison, and resilience.
- **Unified tool definitions**: Tools are defined once via `AIFunctionFactory.Create` and work across all providers — no per-provider tool schemas.
- **Topology flexibility**: The same AI service binary supports both monolith and microservices deployments, controlled by a feature flag.
- **Composable tool sets**: New tool registries can be added without modifying existing ones; duplicate detection prevents silent conflicts.

### Tradeoffs

- **Microsoft.Extensions.AI maturity**: The abstraction is relatively new; edge-case differences between providers (token counting, streaming behavior, tool-calling conventions) may surface.
- **Keyed DI complexity**: Four provider registrations plus three tool registries require careful DI configuration and clear naming conventions.
- **Sliding window simplicity**: The 40-message window is a pragmatic choice but does not perform semantic compression or summarization — long conversations lose early context.

## Implementation Notes

- Provider and model are stored in Redis (DB 1) under `jobboard:config:ai-service-v2:AIProvider` and `AIModel`, synced to `IConfiguration` via Dapr configuration watcher.
- The decorator pipeline mirrors the monolith pattern (see [ADR-005](./ADR-005-CQRS-and-Decorator-Pipeline.md)): `ConversationContextDecorator` → `UserContextCommandHandlerDecorator` → `ObservabilityCommandHandlerDecorator` → `NormalizationCommandHandlerDecorator` → `ValidationCommandHandlerDecorator` → handler.
- Tool handlers are resolved at invocation time via `IAiToolHandlerResolver`, preserving the scoped DI lifetime for per-request concerns (user context, conversation context).
- OpenTelemetry activities tag `ai.provider`, `ai.model`, `ai.tokens.total`, and tool-specific metadata for Jaeger trace analysis (see [ADR-004](./ADR-004-Observability-First.md)).
