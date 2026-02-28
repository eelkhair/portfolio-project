# ADR-011: Strangler-Fig Connector API and Provisioning Sagas

- **Status:** Accepted
- **Date:** 2026-02-28

## Context

The portfolio demonstrates an incremental migration from monolith to microservices (see [ADR-001](./ADR-001-Architecture-Showcase-Scope.md)). When a company or job is created in the monolith, the corresponding data must be **provisioned across multiple microservices** — company-api, job-api, and user-api — each owning a slice of the domain. The user-api additionally provisions Auth0 organizations and user accounts, whose IDs must be fed back to the monolith to activate the company.

This creates a multi-step, cross-service coordination problem with requirements for:
- **Atomicity**: Either all services are provisioned or the failure is visible and retryable.
- **Idempotency**: The outbox processor may deliver the same event more than once (see [ADR-003](./ADR-003-Transactional-Outbox.md)).
- **Observability**: The full provisioning flow must be traceable as a single distributed operation.

Two coordination styles were considered:
1. **Choreography**: Each service listens for events and acts independently. Simple but makes the overall flow implicit and harder to monitor.
2. **Orchestration (saga)**: A central coordinator drives the steps explicitly, with clear phase boundaries and error attribution.

## Decision

### Connector API as Strangler-Fig Coordinator

A dedicated **Connector API** service subscribes to outbox-published integration events from the monolith and orchestrates provisioning across microservices. It acts as the Strangler-Fig transition layer — the monolith publishes events without knowledge of downstream services, and the Connector API handles the fan-out.

### Event-Driven Trigger via Dapr Pub/Sub

The monolith's transactional outbox (see [ADR-003](./ADR-003-Transactional-Outbox.md)) publishes events to RabbitMQ via Dapr. The Connector API subscribes to two topics:

| Topic | Event | Saga |
|-------|-------|------|
| `monolith.company-created.v1` | `CompanyCreatedV1Event` | `CompanyProvisioningSaga` |
| `monolith.job-created.v1` | `JobCreatedV1Event` | `JobProvisioningSaga` |

### CompanyProvisioningSaga — Orchestrated Fan-Out

The company provisioning saga executes four phases, each instrumented as a distinct OpenTelemetry span:

1. **Fetch data** (`provision.company.saga.fetch-data`): Parallel OData queries to the monolith retrieve company details and admin user details — data not included in the lightweight integration event.
2. **Map payloads**: `CompanyCreatedMapper` transforms the fetched data into three service-specific payloads with distinct schemas.
3. **Fan-out** (`provision.company.saga.fan-out`): `Task.WhenAll` dispatches to company-api, job-api, and user-api **in parallel** via Dapr service invocation. The user-api call returns Auth0 organization and user IDs.
4. **Activate** (`provision.company.saga.activate`): Posts the Auth0 IDs back to the monolith's `/companies/company-created-success` endpoint, marking the company as active.

### JobProvisioningSaga — Forward and Republish

The job saga is simpler: it forwards the mapped payload to job-api via Dapr service invocation, then republishes the result as a `job.published.v2` event to RabbitMQ for downstream consumers.

### Idempotency via Dapr State Store

Each endpoint checks a Redis-backed Dapr state key (`Provisioned:{idempotencyKey}`) before executing the saga:

| State | TTL | Meaning |
|-------|-----|---------|
| *(absent)* | — | First delivery; proceed with saga |
| `"processing"` | 120 seconds | Saga in progress; return `202 Accepted` |
| `"done"` | 7 days | Successfully completed; return `202 Accepted` |

The 120-second pending TTL acts as a **processing timeout** — if the saga crashes, the key expires and the next delivery retries. The 7-day completed TTL prevents reprocessing while allowing eventual key cleanup.

### Why Orchestration Over Choreography

For this specific flow, orchestration was chosen because:
- The **activation step depends on results** from the user-api (Auth0 IDs), requiring sequential coordination after the fan-out.
- **Error attribution** is straightforward — the saga knows which phase failed and can log accordingly.
- The flow is **finite and well-defined** (4 phases), not an open-ended event chain.
- **Trace spans** map naturally to saga phases, making the Jaeger trace readable (see [ADR-004](./ADR-004-Observability-First.md)).

Choreography remains appropriate for loosely coupled, fire-and-forget flows elsewhere in the system.

## Consequences

### Positive

- **Explicit coordination**: The saga makes the provisioning flow visible, debuggable, and testable as a single unit.
- **Parallel fan-out**: `Task.WhenAll` reduces provisioning latency by calling three services concurrently.
- **Idempotent by default**: Redis state keys with TTLs handle duplicate deliveries from the outbox processor without additional application logic.
- **Decoupled monolith**: The monolith publishes events without knowing about microservices — the Connector API absorbs all coordination complexity.
- **Traceable end-to-end**: The outbox processor restores `TraceParent` from the outbox record, and the saga adds child spans per phase, producing a complete trace from monolith command → outbox → Connector API → microservices → activation.

### Tradeoffs

- **Single point of coordination**: The Connector API is a critical path for provisioning. If it is down, events queue in RabbitMQ until it recovers — acceptable for this portfolio's scale but would need redundancy in production.
- **No compensation (rollback)**: If the fan-out partially succeeds (e.g., company-api succeeds but user-api fails), the saga does not compensate the successful calls. The idempotency key expires after 120 seconds, allowing a full retry. For production, explicit compensation steps would be warranted.
- **Data re-fetch from monolith**: The saga fetches company/admin details via OData rather than including them in the integration event. This keeps events lightweight but adds a round-trip.

## Implementation Notes

- All microservice calls use **Dapr service invocation** (`DaprClient.CreateInvokeMethodRequest`) with app IDs: `monolith-api`, `company-api`, `job-api`, `user-api`, `admin-api`.
- Typed HTTP clients (`IMonolithClient`, `ICompanyApiClient`, `IJobApiClient`, `IUserApiClient`, `IAdminApiClient`) are registered as scoped services and injected into sagas.
- The outbox processor runs on a 10-second Dapr cron binding, selecting `TOP 20` unprocessed messages with `UPDLOCK, READPAST` locking for concurrency safety.
- Integration events use `Guid.CreateVersion7()` for correlation IDs, preserving temporal ordering.
- The Connector API's Dapr component configuration mirrors the monolith's RabbitMQ pub/sub setup (see [ADR-006](./ADR-006-RabbitMQ-vs-Azure-Service-Bus.md)).
