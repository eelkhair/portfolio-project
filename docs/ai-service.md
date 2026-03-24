## AI Service v2

The AI Service v2 is a .NET 10 Clean Architecture service that provides LLM-powered chat, resume parsing, embedding generation (RAG), and job matching. It replaces the legacy Node.js AI service.

### Chat Flow

A chat request flows through:

1. **Endpoint** — `POST /chat` (Admin), `/chat/company` (CompanyAdmin), `/chat/public` (Public). Each sets a `ChatScope` that determines tool access.
2. **ChatCommandHandler** — creates an OpenTelemetry span, selects the system prompt, prepends company context if scoped.
3. **ChatService.RunChatAsync** — the core chat loop:
   - Resolves the LLM client by reading `AIProvider` config key at runtime
   - Wraps it in `FunctionInvokingChatClient` for automatic tool invocation
   - Loads conversation history from `IConversationStore` (Dapr state)
   - Resolves tools via `ChatOptionsFactory.Create(scope)` — merges MCP tools + local tools based on scope and topology
   - Sets the user's JWT on `McpRequestContext.CurrentToken` (AsyncLocal) so MCP tool calls forward authentication
   - Calls `client.GetResponseAsync()` — the Microsoft.Extensions.AI middleware handles tool call loops automatically
   - Trims conversation to 40 messages, saves to store, returns response with TraceId

### Chat Scopes and Tool Access

Each scope resolves a different set of tools:

| Scope | Auth Policy | MCP Tools | Local Tools |
|-------|-----------|-----------|-------------|
| **Admin** | `AdminChat` (groups=Admins) | monolith-mcp OR admin-api-mcp (based on topology) | `AdminToolRegistry`: trace_id, conversation_id, provider_retrieval, generate_draft, is_monolith, set_mode |
| **CompanyAdmin** | `CompanyAdminChat` (groups=Admins,CompanyAdmins) | — | `CompanyAdminToolRegistry` (empty, planned) |
| **Public** | `PublicChat` (groups=Admins,CompanyAdmins,Applicants) | monolith-mcp OR admin-api-mcp (based on topology) | `PublicToolRegistry` (empty, planned) |

Tool selection between monolith-mcp and admin-api-mcp is driven by the `FeatureFlags:Monolith` config key — when true, tools come from monolith-mcp; when false, from admin-api-mcp.

### System Prompts

- **Admin/CompanyAdmin** — enforces wizard-style field collection for mutations, requires review/confirmation before writes, treats tool responses as the sole source of truth
- **Public** — helpful job-seeker assistant, no internal system details exposed

### Multi-Provider LLM Support

Four providers registered as keyed `IChatClient` singletons:

| Provider | DI Key | Config Keys |
|----------|--------|-------------|
| OpenAI | `"openai"` | `AI:OPENAI_API_KEY`, `AI:OPENAI_MODEL` |
| Azure OpenAI | `"azure"` | `AI:AZURE_API_KEY`, `AI:AZURE_API_Endpoint`, `AI:AZURE_OPENAI_MODEL` |
| Claude | `"claude"` | `AI:CLAUDE_API_KEY` |
| Gemini | `"gemini"` | `AI:GEMINI_API_KEY`, `AI:GEMINI_MODEL` |

The active provider is selected at runtime via the `AIProvider` config key (changeable via Redis config, Settings API, or the `set_mode` chat tool). API keys are stored in .NET User Secrets locally.

Embeddings use OpenAI's `text-embedding-3-small` model (1536 dimensions), registered separately as `"openai.embedding"`.

### MCP Integration

MCP (Model Context Protocol) servers expose monolith and admin-api operations as LLM-callable tools.

**Connection lifecycle:**
- `McpToolProvider` lazily connects on first `GetTools()` call (not at startup), so the API starts even if MCP servers are down
- Uses `SemaphoreSlim` for thread-safe double-checked locking on connection creation
- On failure, resets the client so the next call retries
- `McpClientTool` extends `AITool`, so tools integrate directly into `ChatOptions.Tools`

**Authentication forwarding:**
- `McpRequestContext.CurrentToken` (static `AsyncLocal<string>`) flows the user's JWT from the chat request to MCP HTTP calls
- `UserTokenForwardingHandler` reads from this AsyncLocal and sets `X-Forwarded-Authorization` on outbound requests
- Token is set before LLM invocation and cleared in a `finally` block

**Registration:**
- `McpServer:IntegrationUrl` → monolith-mcp (registered as `"admin-monolith"` and `"public-monolith"`)
- `McpServer:MicroUrl` → admin-api-mcp (registered as `"admin-micro"` and `"public-micro"`)

### Resume Embedding Pipeline (RAG)

A three-stage event-driven pipeline triggered by the monolith's transactional outbox:

**Stage 1: Parse** (`monolith.resume-uploaded.v1`)
1. Downloads resume from Azure Blob Storage
2. Extracts text via `ResumeTextExtractor`
3. **Phase 1 (quick)**: Parses contact info, summary, skills via LLM. Backfills email/phone from regex if LLM missed them.
4. **Phase 2 (parallel)**: Four sections parsed concurrently via `Task.WhenAll` — work history, education, certifications, projects. Each section has up to 2 retry attempts.
5. Each parsed section streams back to the monolith via `NotifySectionParsedAsync` → SignalR to the frontend
6. On completion, triggers the embedding stage

**Stage 2: Embed** (`monolith.resume-parsed.v1`)
1. Fetches full parsed content from monolith
2. Builds section-specific texts (full, skills, experience)
3. Batch-embeds all texts via `EmbeddingService` (1536-dim vectors)
4. Upserts `ResumeEmbedding` entity in pgvector
5. Invalidates stale `MatchExplanation` records
6. Fire-and-forget: pre-computes match explanations for top matching jobs

**Stage 3: Delete** (`monolith.resume-deleted.v1`)
- Removes embedding and match explanation records from pgvector

All event handlers use idempotency via Dapr state store — processed event keys are tracked to prevent duplicate processing. Handlers always return `Accepted()` to prevent Dapr retries.

### API Endpoints

| Route | Method | Auth | Description |
|-------|--------|------|-------------|
| `/chat` | POST | AdminChat | Admin chat with full tool access |
| `/chat/company` | POST | CompanyAdminChat | Company-scoped chat |
| `/chat/public` | POST | PublicChat | Applicant chat |
| `/resumes/parse` | POST | JWT | Synchronous resume parse (Dapr invoke) |
| `/resumes/parse-event` | POST | DaprInternal | Pub/sub: resume uploaded |
| `/resumes/embed-event` | POST | DaprInternal | Pub/sub: resume parsed |
| `/resumes/delete-embedding-event` | POST | DaprInternal | Pub/sub: resume deleted |
| `/resumes/{id}/matching` | GET | DaprInternal | Matching jobs for a resume |
| `/jobs/publish` | POST | DaprInternal | Pub/sub: job published |
| `/jobs/{id}/similar` | GET | DaprInternal | Similar jobs |
| `/jobs/search` | GET | DaprInternal | Semantic job search |
| `/drafts/{companyId}/generate` | POST | JWT | Generate AI job draft |
| `/drafts/rewrite/item` | PUT | JWT | Rewrite a draft item |
| `/settings/provider` | GET/PUT | JWT | Get/set AI provider |
| `/settings/mode` | GET/PUT | JWT | Get/set application mode |
| `/settings/re-embed-jobs` | POST | JWT | Re-embed all jobs |
| `/settings/generate-match-explanations` | POST | JWT | Generate match explanations |

### Health Checks

The AI service exposes comprehensive health checks at `/healthz`:
- **PostgreSQL** (pgvector database)
- **4 AI providers** (OpenAI, Azure OpenAI, Anthropic, Gemini) — validates API reachability and credentials
- **Keycloak** — realm and client configuration
- **Dapr** — sidecar, state store, secret store, pub/sub

### Observability

- OpenTelemetry spans on every chat command with tags: `chat.scope`, `ai.provider`, `ai.model`, `ai.tokens.*`, `conversation_id`
- Token usage tracked per request (prompt tokens, completion tokens, total)
- Resume pipeline spans correlate back to the original upload via stored `TraceParent`
- SignalR notifications include `traceParent`/`traceState` for frontend trace correlation

### Related ADRs

- [ADR-009 — Multi-Provider Function Calling](ADRs/ADR-009-AI-Service-Multi-Provider-Function-Calling.md)
- [ADR-010 — Real-Time AI Notifications SignalR](ADRs/ADR-010-Real-Time-AI-Notifications-SignalR.md)
- [ADR-014 — Resume Embedding Pipeline (RAG)](ADRs/ADR-014-Resume-Embedding-Pipeline-RAG.md)
- [ADR-015 — MCP Server Integration](ADRs/ADR-015-MCP-Server-Integration.md)
- [ADR-016 — Multi-Scope AI Chat](ADRs/ADR-016-Multi-Scope-AI-Chat.md)
