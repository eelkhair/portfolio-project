# Strangler-Fig Migration (Connector API)

This project demonstrates a **safe, incremental migration** strategy using the **Strangler Fig** pattern.

Instead of rewriting everything at once:
- keep the monolith as the system of record
- introduce a transition layer (**Connector API**)
- gradually extract capabilities into microservices
- reduce risk while improving modularity over time

---

## Why this exists

In many real systems you can't pause the business for a rewrite.
This architecture is designed to show:
- **coexistence** (monolith + services)
- **incremental extraction**
- **operational safety** (idempotency, retries, DLQs, observability)

---

## The stages

### Stage 1 — Monolith is primary
The monolith owns most behaviors and data.

Screenshot:
- ![images/Strangler Fig/strangler-fig-stage-1-monolith-primary.png](images/Strangler%20Fig/strangler-fig-stage-1-monolith-primary.png)

### Stage 2 — Introduce Connector API
A Connector API becomes a controlled transition layer:
- routes some calls to the monolith
- routes extracted operations to microservices
- orchestrates workflows spanning both

Screenshot:
- ![images/Strangler Fig/strangler-fig-stage-2-connector-introduced.png](images/Strangler%20Fig/strangler-fig-stage-2-connector-introduced.png)

### Stage 3 — Extract services gradually
Capabilities move from monolith modules/endpoints into services.
The Connector:
- keeps the public contract stable
- coordinates between old and new

Screenshot:
- ![images/Strangler%20Fig/strangler-fig-stage-3-service-extraction.png](images/Strangler%20Fig/strangler-fig-stage-3-service-extraction.png)

### Stage 4 — Deprecate monolith paths
After confidence is high, legacy paths are removed or disabled.
The monolith becomes slimmer or can be retired for that slice.

Screenshot:
- `images/Strangler%20Fig/strangler-fig-stage-4-monolith-deprecated-paths.png`

---

## Connector API responsibilities

- **Routing**: decide whether to call monolith or service
- **Orchestration**: multi-step workflows that touch multiple systems
- **Idempotency**: ensure retries don't duplicate effects
- **Observability**: preserve trace context across calls and pub/sub
- **Async workflows**: publish/consume events for cross-service coordination

---

## Async workflow example (company provisioning)

A typical flow includes:
1. Admin UI calls Monolith API (or Admin API)
2. An event is written to an outbox and published
3. Connector API consumes the event and kicks off a saga
4. Downstream services handle their bounded responsibilities
5. Completion events are published and observed end-to-end

What matters is not the exact business flow — it's the **visibility and safety**:
- correlated TraceId
- saga step logs
- DLQ paths
- idempotent consumers

---

## Operational evidence

These screenshots are useful when explaining the approach to reviewers:

- Centralized logs: `images/Strangler Fig/strangler-fig-centralized-logs-elastic.png`
- Saga log sequence: `images/Strangler Fig/strangler-fig-saga-log-sequence.png`

---

## When to use this pattern

Use strangler fig when:
- you have a stable monolith serving production needs
- you want to modernize without “stop the world”
- you need a low-risk path to service extraction
- you need to keep external contracts stable while internals evolve
