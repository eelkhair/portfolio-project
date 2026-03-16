# ADR-012: Reverse Connector API — Bidirectional Strangler-Fig Sync

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The Strangler-Fig Connector API ([ADR-011](./ADR-011-Strangler-Fig-Connector-API-Provisioning-Sagas.md)) established a one-way data flow: monolith publishes integration events, and the Connector API fans out provisioning across microservices. However, the AI chat layer (via MCP tools) writes directly to microservices — creating companies, jobs, and drafts that the monolith never sees.

This one-way flow created concrete problems:
- **Data divergence**: Companies created via the AI chat existed in microservices but not the monolith. The monolith's OData queries returned stale results, and the admin UI showed incomplete data.
- **UId mismatches**: The monolith assigns `InternalId` + `Id` pairs via SQL sequences, while microservices use `Id` + `UId` with `newsequentialid()`. Without reverse sync, the monolith had no record to correlate against.
- **Missing audit fields**: The monolith's `BaseAuditableEntity` auto-populates `CreatedBy`/`UpdatedBy` via `SaveChangesAsync(userId)`. Data created externally lacked this audit trail.

Two approaches were considered:
1. **Force all writes through the monolith**: Route AI chat tool calls to the monolith instead of microservices. Simple but defeats the purpose of the Strangler-Fig migration — the microservices would become read-only replicas.
2. **Reverse sync service**: A dedicated service subscribes to microservice events and replays them to the monolith, closing the bidirectional loop.

## Decision

### Dedicated Reverse Connector API

A new **reverse-connector-api** service mirrors the forward Connector API but in the opposite direction. It subscribes to microservice-originated integration events via Dapr pub/sub (RabbitMQ) and calls dedicated `/api/sync/*` endpoints on the monolith to replay changes.

### Event-Driven Trigger via Dapr Pub/Sub

The reverse connector subscribes to five topics on the `micro.*` namespace:

| Topic | Event | Monolith Sync Endpoint |
|-------|-------|------------------------|
| `micro.draft-saved.v1` | `DraftSavedV1Event` | `POST /api/sync/drafts` |
| `micro.draft-deleted.v1` | `DraftDeletedV1Event` | `DELETE /api/sync/drafts/{uid}` |
| `micro.company-created.v1` | `MicroCompanyCreatedV1Event` | `POST /api/sync/companies` |
| `micro.company-updated.v1` | `MicroCompanyUpdatedV1Event` | `PUT /api/sync/companies/{uid}` |
| `micro.job-created.v1` | `MicroJobCreatedV1Event` | `POST /api/sync/jobs` |

### Sync Endpoint Pattern

Each endpoint follows a consistent pattern:

1. **Start OpenTelemetry span**: `reverse-sync.{resource}.{action}` with `userId` and resource identifiers as tags.
2. **Idempotency check**: Redis-backed Dapr state with key `{action}:{idempotencyKey}`. Same TTL strategy as the forward Connector API — 120-second `"processing"` window, 7-day `"done"` retention.
3. **Map payload**: Dedicated mappers (`CompanyMapper`, `DraftMapper`, `JobMapper`) transform microservice event schemas into monolith sync payloads.
4. **HTTP sync**: `MonolithHttpClient` calls the monolith's `/api/sync/*` endpoints with an `X-Api-Key` header for internal authentication.
5. **Mark complete**: Update Redis state to `"done"`.

### Why a Separate Service (Not Embedded in Connector API)

- **Single responsibility**: The forward connector handles monolith-to-microservices fan-out with orchestrated sagas. The reverse connector handles microservices-to-monolith replay with simpler 1:1 mappings. Merging them would create a bidirectional coupling point.
- **Independent scaling and deployment**: The reverse connector can be deployed, versioned, and scaled independently. If the forward connector has issues, reverse sync continues unaffected.
- **Clear event namespace separation**: Forward events use `monolith.*` topics; reverse events use `micro.*` topics. Each connector subscribes to exactly one namespace.

### Monolith Sync Endpoints

The monolith exposes `/api/sync/*` endpoints that bypass the normal command pipeline's user-context validation. These endpoints:
- Accept the `userId` from the event payload (the original actor who made the change in microservices)
- Use the `InternalOrJwt` auth policy (accepts `X-Api-Key`)
- Create or update domain entities with proper audit fields populated

## Consequences

### Positive

- **Closed data loop**: Both directions of data flow are handled — monolith changes propagate to microservices (forward connector), and microservice changes propagate to the monolith (reverse connector). The admin UI and OData queries reflect all writes regardless of entry point.
- **Consistent architecture**: The reverse connector reuses the same patterns as the forward connector — Dapr pub/sub, idempotency via Redis state, OpenTelemetry tracing, typed HTTP clients — making the codebase predictable.
- **Shared event contracts**: Both connectors consume events from the same `JobBoard.IntegrationEvents` NuGet package, ensuring schema consistency.
- **Audit trail preserved**: The original `userId` is forwarded from microservice events, so the monolith's audit fields correctly attribute changes to the actual user.

### Tradeoffs

- **Eventual consistency window**: Changes made in microservices are not immediately visible in the monolith. The delay includes event publishing, RabbitMQ delivery, and sync processing — typically sub-second but unbounded under load.
- **No conflict resolution**: If the same entity is modified simultaneously in both the monolith and a microservice, the last write wins. For this portfolio's scale, this is acceptable; production systems would need vector clocks or CRDTs.
- **Additional service to operate**: The reverse connector adds a service, Dapr sidecar, and health check to the deployment topology. The operational cost is justified by the data consistency it provides.
- **Monolith sync endpoints are a new API surface**: The `/api/sync/*` endpoints must be maintained alongside the standard `/api/*` endpoints and kept in sync with schema changes.

## Implementation Notes

- The reverse connector uses a flat endpoint structure (no CQRS or decorator pipeline) — each Dapr subscription maps directly to a handler method in a minimal API endpoint class.
- `MonolithHttpClient` is a typed `HttpClient` configured with `MonolithUrl` (defaults to `http://monolith-api:8080`) and optional `InternalApiKey` header.
- The `DraftMapper` reconstructs the monolith's expected JSON structure from individual event fields, since drafts store content as a JSON blob in the monolith.
- All five event types use the shared `IIntegrationEvent` base interface, with `Micro*` prefixed records for events originating from microservices (see [ADR-017](./ADR-017-IntegrationEvents-Shared-NuGet-Package.md)).
- Configuration is loaded from Redis via Dapr app-config component, and secrets from HashiCorp Vault — same pattern as all other services.
