# ADR-003: Transactional Outbox for Reliable Integration Events

- **Status:** Accepted
- **Date:** 2026-01-03

## Context

In systems that publish integration events (to a message bus, broker, or pub/sub), publishing directly after saving data can lead to inconsistencies:
- DB commit succeeds but event publish fails
- event publish succeeds but DB commit fails
- retries cause duplicate publishes

We want a reliable pattern that preserves **atomicity** between business state change and the intent to publish an event.

## Decision

We will implement a **Transactional Outbox**:

- During the same database transaction as the state change, we persist an outbox record representing the integration event.
- A background processor reads undispatched outbox records and publishes them to the integration mechanism (e.g., Dapr pub/sub, broker).
- After successful publish, the outbox record is marked as dispatched (and may be retained for audit or cleaned up by retention policy).

Idempotency:
- Consumers should be idempotent.
- Events should include stable identifiers (event id, aggregate id, version/timestamp) to support deduplication.

## Consequences

### Positive
- Strong reliability guarantees (no “lost events”).
- Enables controlled retries and backoff strategies.
- Clear separation between business transaction and integration.

### Negative / Tradeoffs
- Adds an outbox table and background processing.
- Slight delay between commit and event delivery (eventual consistency).
- Requires operational monitoring (lag, failures).

## Implementation Notes

- Outbox writes occur in the same unit-of-work/transaction as domain changes.
- Publishing uses retry policies and emits telemetry.
- Health checks include outbox processor status/lag where appropriate.
