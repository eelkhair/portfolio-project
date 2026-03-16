# ADR-016: Multi-Scope AI Chat (Admin / CompanyAdmin / Public)

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The AI chat system serves three distinct user audiences with different authorization levels, tool access, and conversational goals:

1. **Admins**: Platform operators who manage all companies, jobs, drafts, and system configuration.
2. **Company Admins**: Company-scoped users who manage their own company's resources but cannot access other companies or system settings.
3. **Applicants (Public)**: Job seekers who search for opportunities and get career guidance but have no write access to platform data.

A single chat endpoint with role-based filtering would work, but it creates a complex conditional chain inside the handler and makes it difficult to reason about what each audience can do. The tool set, system prompt, and authorization boundary differ significantly between scopes.

## Decision

### Three Chat Endpoints with Scope Enum

Each audience gets a dedicated endpoint with its own authorization policy:

| Endpoint | Policy | Keycloak Groups | ChatScope |
|----------|--------|-----------------|-----------|
| `POST /chat` | `AdminChat` | `Admins` | `Admin` |
| `POST /chat/company` | `CompanyAdminChat` | `Admins`, `CompanyAdmins` | `CompanyAdmin` |
| `POST /chat/public` | `PublicChat` | `Admins`, `CompanyAdmins`, `Applicants` | `Public` |

All three endpoints dispatch the same `ChatCommand` but with a different `ChatScope` enum value. The scope drives tool resolution and system prompt selection downstream.

### Scope-Driven Tool Resolution

`IChatOptionsFactory.Create(sp, ChatScope)` resolves different tool sets per scope:

**Admin scope**:
- Topology tools (via MCP — monolith or microservices, based on feature flag)
- AI-specific tools (`TraceIdTool`, `ConversationIdTool`, `ProviderRetrievalTool`, `GenerateDraftTool`, `IsMonolithTool`, `SetModeTool`)
- Duplicate detection across merged tool lists

**CompanyAdmin scope**:
- Company-scoped tool registry (currently empty — tools to be added as company-admin features are built)

**Public scope**:
- Topology tools (same MCP servers as admin, but keyed separately for future filtering)
- Public AI tools (currently empty — search and matching tools planned)
- Duplicate detection across merged tool lists

### Keyed DI for Tool Registries

Tool registries are registered as keyed services in DI, enabling scope-specific resolution without conditional logic:

| DI Key | Registry | Scope |
|--------|----------|-------|
| `admin-ai` | `AdminToolRegistry` | Admin |
| `admin-monolith` / `admin-micro` | `McpToolProvider` | Admin (topology) |
| `company-admin` | `CompanyAdminToolRegistry` | CompanyAdmin |
| `public-ai` | `PublicToolRegistry` | Public |
| `public-monolith` / `public-micro` | `McpToolProvider` | Public (topology) |

The `ChatOptionsFactory` resolves the correct keyed services based on the `ChatScope`, merges them, and returns a `ChatOptions` with the combined tool list.

### Scope-Specific System Prompts

Two system prompts serve different conversational styles:

**AdminSystemPrompt** (Admin and CompanyAdmin scopes):
- Tool responses are the only source of truth
- Must not invent, infer, or estimate facts
- Must call a tool before answering data questions
- Includes rules for counting, freshness, failure handling, confirmation flow, and cancellation

**PublicChatSystemPrompt** (Public scope):
- Helpful, concise, professional tone
- Answers general job search, resume, and career questions
- Acknowledges limitations (personalized matching coming soon)
- Never exposes internal system details or IDs

### Why Separate Endpoints (Not a Query Parameter)

The scope could be passed as a query parameter or request body field, with a single endpoint checking authorization dynamically. Separate endpoints were chosen because:

- **Authorization is declarative**: `[Authorize(Policy = "AdminChat")]` on the endpoint is enforced by the framework before the handler runs. A single endpoint with dynamic policy checks is error-prone.
- **API documentation**: Swagger generates distinct entries for each endpoint with clear descriptions of who can call what.
- **Rate limiting**: Future rate limits can be applied per-endpoint (e.g., public chat may have stricter limits than admin chat).
- **Frontend routing**: The Angular admin app calls `/chat`, while the public app will call `/chat/public`. No conditional logic in the frontend.

## Consequences

### Positive

- **Least-privilege by design**: Each scope only sees tools appropriate to its authorization level. An applicant cannot accidentally invoke admin tools because they're never loaded.
- **Independent evolution**: CompanyAdmin and Public tool registries are currently empty placeholders. New tools can be added to these registries without touching admin functionality.
- **Topology-aware**: Both Admin and Public scopes support monolith/microservices switching via feature flag, inheriting the MCP topology pattern ([ADR-015](./ADR-015-MCP-Server-Integration.md)).
- **Testable isolation**: Each scope's tool set can be unit tested independently by resolving the keyed DI service.

### Tradeoffs

- **Three endpoints to maintain**: Any change to the chat flow (e.g., adding conversation history, modifying response format) must be reflected in the shared `ChatCommand` handler, not in endpoint-specific code. The endpoints themselves are thin.
- **Empty registries**: CompanyAdmin and Public registries return no tools yet. The infrastructure is in place, but the actual company-scoped and public-facing tools are future work.
- **Shared MCP tools across scopes**: Admin and Public currently resolve the same MCP server tools. If public users should see a subset of tools (e.g., read-only), the MCP servers would need scope-aware filtering or separate tool type registrations.

## Implementation Notes

- `ChatScope` is a simple enum: `Admin`, `CompanyAdmin`, `Public`.
- `ChatOptions` includes `MaxOutputTokens = 5000`, `Temperature = 1`, and `ModelId` from configuration.
- The `ChatService` sets `McpRequestContext.CurrentToken` before invoking `FunctionInvokingChatClient` and clears it in a `finally` block to prevent token leakage across requests.
- Conversations are stored in Redis with a sliding 40-message window, keyed by `conversationId`.
- Admin tools include `TraceIdTool` (returns the current OpenTelemetry trace ID for debugging), `IsMonolithTool` (reports current topology mode), and `SetModeTool` (toggles the monolith feature flag at runtime).
