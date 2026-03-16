# ADR-015: MCP Server Integration (Model Context Protocol)

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The AI service v2 uses function calling to let the LLM interact with backend data — listing companies, creating drafts, publishing jobs. Initially, these tools were implemented as in-process `AITool` instances that called backend APIs via typed HTTP clients. This worked but created tight coupling: the AI service needed to know every API endpoint, DTO schema, and authentication flow for both the monolith and microservices.

As the tool surface grew, maintaining duplicate API client code across topologies (monolith vs. microservices) became expensive. Adding a new tool required changes in the AI service even though the business logic lived in the backend.

Three approaches were considered:
1. **In-process tools with typed HTTP clients**: Current approach. Simple but duplicates API knowledge in the AI service.
2. **OpenAPI-driven tool generation**: Auto-generate tools from Swagger specs. Reduces duplication but produces generic, poorly described tools that confuse the LLM.
3. **MCP (Model Context Protocol) servers**: Backend services expose tools via a standard protocol. The AI service discovers and invokes them dynamically. Tools are defined where the business logic lives.

## Decision

### Dedicated MCP Server Processes

Two MCP server projects expose domain tools from the backend:

| Server | Port | Source of Truth | Tools |
|--------|------|-----------------|-------|
| `monolith-mcp` (3333) | Monolith database | Companies, Jobs, Drafts, Industries |
| `admin-api-mcp` (3334) | Microservices databases | Same tool names, backed by microservice data |

Both servers expose **identical tool interfaces** (same names, same parameters) but route to different data stores. This enables the AI service to switch between monolith and microservices transparently via a feature flag.

### Why Separate Processes (Not Embedded)

MCP servers run as independent ASP.NET Core applications rather than being embedded in the monolith or admin-api:

- **Isolation**: MCP tool execution is stateless and request-scoped. Separating it from the main API prevents tool-call load from affecting user-facing endpoints.
- **Independent deployment**: Tool descriptions and behavior can be updated without redeploying the main API.
- **Protocol boundary**: The MCP HTTP transport (`ModelContextProtocol.AspNetCore`) requires its own routing pipeline. Embedding it in a controller-based API would create routing conflicts.

### Stateless HTTP Transport

Both MCP servers use stateless HTTP transport (`transport.Stateless = true`). Each HTTP request creates a fresh DI scope — there's no session state between tool calls. This simplifies deployment (no sticky sessions) and matches the AI service's request-per-tool-call pattern.

### Tool Implementation Pattern

Tools are defined using `[McpServerToolType]` and `[McpServerTool]` attributes. In the monolith MCP, tools dispatch through the full CQRS decorator pipeline via a `HandlerDispatcher`:

```
MCP Tool → HandlerDispatcher → Decorator Pipeline → Core Handler → Database
```

This means every tool call goes through:
1. **UserContextCommandHandlerDecorator** — auth check + user sync
2. **ObservabilityCommandHandlerDecorator** — tracing
3. **ValidationCommandHandlerDecorator** — FluentValidation
4. **TransactionCommandHandlerDecorator** — DB transaction
5. **ExceptionHandlingCommandHandlerDecorator** — error conversion

Tools are not a backdoor — they execute with the same validation, authorization, and observability as API endpoints.

### Dynamic Tool Discovery

The AI service discovers tools at runtime via `McpToolProvider`, which connects to MCP servers using the `McpClient` from the official MCP SDK:

1. On first `GetTools()` call, the provider establishes an HTTP connection to the MCP server
2. `ListToolsAsync()` returns all available tools with names, descriptions, and JSON schemas
3. Tools are returned as `AITool` instances compatible with `Microsoft.Extensions.AI`'s `FunctionInvokingChatClient`

Tool discovery is lazy — the MCP client connects on first use, not at startup. This prevents startup failures if an MCP server is temporarily unavailable.

### User Token Forwarding

MCP tool calls must execute in the context of the authenticated user. The AI service forwards the user's JWT using an `AsyncLocal`-based context:

1. `ChatService` extracts the user's token from `IUserAccessor` and sets `McpRequestContext.CurrentToken`
2. `UserTokenForwardingHandler` (a `DelegatingHandler`) reads the `AsyncLocal` token and adds it as `X-Forwarded-Authorization` header
3. `ForwardedAuthMiddleware` on the MCP server copies `X-Forwarded-Authorization` → `Authorization`
4. Keycloak JWT bearer validation authenticates the user in the MCP server's DI scope
5. `IUserAccessor` is populated, and the tool executes with the user's identity

This preserves the user's authorization context across the AI service → MCP server boundary without the AI service needing to re-authenticate.

### Topology Switching via Feature Flag

The AI service selects between monolith and microservices MCP servers using `FeatureFlags:Monolith`:

| Flag Value | Admin Tools Source | Public Tools Source |
|------------|-------------------|---------------------|
| `true` | `admin-monolith` (monolith-mcp:3333) | `public-monolith` (monolith-mcp:3333) |
| `false` | `admin-micro` (admin-api-mcp:3334) | `public-micro` (admin-api-mcp:3334) |

Since both MCP servers expose identical tool names, the LLM's behavior is unchanged when switching topologies. This enables incremental migration testing — toggle the flag, verify the AI chat produces the same results, toggle back if issues arise.

### Duplicate Tool Detection

When the `ChatOptionsFactory` merges topology tools (from MCP) with AI-specific tools (in-process), it checks for name collisions:

```
var duplicates = allTools.GroupBy(t => t.Name).Where(g => g.Count() > 1);
if (duplicates.Any()) throw new InvalidOperationException(...);
```

This catches configuration errors early — e.g., if both MCP servers are accidentally registered for the same scope.

## Consequences

### Positive

- **Tools live where the logic lives**: Adding a new tool means adding a `[McpServerTool]` method in the backend project, not modifying the AI service. The AI service discovers it automatically.
- **Transparent topology migration**: The same AI chat experience works against monolith or microservices data, controlled by a single feature flag.
- **Full pipeline enforcement**: Monolith MCP tools go through the CQRS decorator pipeline — validation, transactions, observability are not bypassed.
- **Standard protocol**: MCP is an open standard. The tools are testable with MCP Inspector and compatible with any MCP client, not just the custom AI service.
- **Auth context preserved**: User identity flows through the entire chain, so tool execution respects the same authorization rules as direct API calls.

### Tradeoffs

- **Two additional services**: `monolith-mcp` and `admin-api-mcp` add to the deployment topology. Each needs its own port, health check, and (for admin-api-mcp) a Dapr sidecar.
- **Network hop latency**: Each tool call adds an HTTP round-trip from AI service → MCP server. For the typical 2-5 tool calls per chat message, this adds ~50-200ms total.
- **Tool description quality matters**: The LLM selects tools based on their `[Description]` attributes. Vague descriptions cause incorrect tool selection or N+1 call patterns. Descriptions must explicitly state when to use each tool and when to prefer aggregates over per-item detail calls.
- **Lazy connection risk**: If the MCP server is down when `GetTools()` is first called, the tool list is empty for that scope. The provider logs a warning but doesn't retry until the next request.

## Implementation Notes

- **Shared library**: `JobBoard.Mcp.Common` (NuGet v1.0.0) provides `KeycloakAuthExtensions`, `IUserAccessor`, `HttpUserAccessor`, and `ForwardedAuthMiddleware` — shared between both MCP servers.
- **MCP SDK**: `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` v1.1.0.
- **CORS**: Both MCP servers enable CORS for MCP Inspector debugging during development.
- **Configuration**: `McpServer:IntegrationUrl` and `McpServer:MicroUrl` in the AI service point to the MCP server endpoints. In Docker Compose, these resolve via Docker DNS (e.g., `http://monolith-mcp:3333`).
