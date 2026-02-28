# ADR-010: Real-Time AI Notifications via SignalR

- **Status:** Accepted
- **Date:** 2026-02-28

## Context

When the AI assistant executes tools that create or modify domain entities (generating a draft, publishing a job), the user needs **immediate feedback** beyond the chat response. The chat reply confirms intent, but the UI must also navigate to the new entity, refresh data stores, and provide visual confirmation — all before the LLM finishes its multi-turn tool-calling loop.

Three approaches were considered:

1. **Polling**: Simple but introduces latency, wasted requests, and poor UX for infrequent events.
2. **Return results only in chat response**: The LLM would describe what happened, but the Angular app has no structured signal to trigger navigation or data refresh.
3. **Server-pushed notifications via SignalR**: The tool sends a structured notification the moment it completes; the frontend reacts immediately with UI updates.

The monolith already uses SignalR for company activation notifications (see [ADR-007](./ADR-007-Trace-Context-Propagation.md)), establishing a precedent for this pattern.

## Decision

### SignalR Hub with User-Scoped Groups

The AI service exposes a SignalR hub at `/hubs/notifications` (requiring authorization). On connection, the hub extracts the user's `sub` claim (Auth0) and adds the connection to a **group named after the user ID**. This enables targeted delivery — notifications reach only the user who initiated the AI action, even across multiple browser tabs.

Authentication reuses the same JWT as the REST API. For SignalR's WebSocket transport, the access token is passed via query string and extracted in `JwtBearerEvents.OnMessageReceived`.

### AiNotificationDto Envelope

Every notification carries a structured envelope:

| Field | Purpose |
|-------|---------|
| `Type` | Notification kind (`draft.generated`, `job.published`) |
| `Title` | Human-readable entity name |
| `EntityId` / `EntityType` | Identifies the created/modified entity |
| `TraceParent` | W3C Trace Context parent ID from `Activity.Current` |
| `TraceState` | W3C Trace Context vendor state |
| `CorrelationId` | Activity trace ID for end-to-end correlation |
| `Metadata` | Extensible dictionary (e.g., `companyId`, `companyName`) |
| `Timestamp` | Execution time |

### Trace Context Propagation

The notification carries the backend `Activity`'s W3C Trace Context (`TraceParent` + `TraceState`). On the frontend, `AiRealtimeService` extracts these fields into a carrier, calls `propagation.extract()` to reconstruct the parent context, and starts a **CONSUMER span** linked to the backend producer span. This creates an unbroken trace from backend tool execution → SignalR delivery → frontend UI action, visible in Jaeger as a single distributed trace.

### Frontend Dispatch Pattern

`AiRealtimeService` dispatches by notification type within the consumer span:

- **`draft.generated`**: Shows a toast, selects the company in the store, loads drafts, and navigates to `/jobs/new/{draftId}`.
- **`job.published`**: Shows a toast, selects the company, refreshes the jobs list, and navigates to `/jobs`.

All UI side effects (navigation, data loading) are captured as child spans with `ui.action` and `ui.route` attributes.

## Consequences

### Positive

- **Immediate UI feedback**: Users see navigation and data updates the moment a tool completes, without waiting for the full chat response.
- **End-to-end trace continuity**: A single trace ID connects the backend tool invocation, SignalR message delivery, and frontend UI reaction — critical for debugging asynchronous AI workflows (see [ADR-004](./ADR-004-Observability-First.md)).
- **Targeted delivery**: User-scoped groups prevent notification leakage across users and reduce unnecessary client processing.
- **Extensible**: New notification types require only a new `Type` value and a frontend handler — no protocol changes.

### Tradeoffs

- **SignalR connection overhead**: Each authenticated user maintains a persistent WebSocket connection to the AI service. This is appropriate for the admin app's limited user base but would need scaling consideration (Azure SignalR Service) for high-concurrency scenarios.
- **Trace context in payload**: Embedding `TraceParent`/`TraceState` in the message body is a pragmatic choice since SignalR does not natively propagate W3C Trace Context headers over WebSocket frames. This is a custom convention, not a standard.
- **Frontend OpenTelemetry dependency**: The Angular app must include `@opentelemetry/api` and configure a tracer to participate in distributed tracing. This adds bundle size and initialization complexity.

## Implementation Notes

- The `AiNotificationHubNotifier` wraps `IHubContext<AiNotificationHub>` and instruments every send with a `signalr.message.send` activity (kind: `Producer`), tagging `messaging.system`, `messaging.destination.name`, `enduser.id`, and `correlation.id`.
- The frontend uses `withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` for resilient reconnection with escalating backoff.
- Both the AI service and the monolith use identical trace propagation patterns for their respective SignalR hubs, ensuring consistency across the platform.
- Tool notifications are fire-and-forget from the tool's perspective — delivery failure is logged but does not fail the tool execution or the chat response.
