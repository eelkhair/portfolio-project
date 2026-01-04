# Job Board Platform (Portfolio)

A **distributed Job Board platform** built to showcase modern backend architecture, event-driven workflows, and production-grade observability.

This repository demonstrates **three architectural evolution paths side-by-side**:

1. **Monolith API** – Clean Architecture + DDD + CQRS
2. **Microservices** – service-per-bounded-context
3. **Strangler-Fig transition layer** via a **Connector API** (incremental migration)

Architectural decisions are documented explicitly using **Architecture Decision Records (ADRs)** to capture trade-offs, constraints, and rationale.

📁 **ADR Index:** [`docs/ADRs`](./ADRs)

---

## Table of Contents

- [What you can do in the system](#what-you-can-do-in-the-system)
- [Architecture at a glance](#architecture-at-a-glance)
- [Key patterns](#key-patterns)
- [Observability](#observability)
- [Health checks](#health-checks)
- [Running locally](#running-locally)
- [Architecture Decision Records](#architecture-decision-records)
- [Screenshots](#screenshots)
- [Roadmap](#roadmap)
- [Notes](#notes)

---

## What you can do in the system

- Create and manage **companies**, **jobs**, and related workflows
- Provision company users via an external identity provider
- Trigger asynchronous workflows via pub/sub and trace them end-to-end
- Generate and rewrite job descriptions using an AI service (optional)

---

## Architecture at a glance

**Frontend**
- **Admin UI (SPA)** calling either the Monolith API or Admin API

**APIs**
- **Monolith API** – primary application for the monolith path
- **Connector API** – strangler layer coordinating monolith and services
- **Admin / Company / Job / User APIs** – microservice bounded contexts

**Infrastructure**
- **SQL Server** – transactional persistence
- **RabbitMQ** – local pub/sub with topics and DLQs
- **Redis** – state, config, optional caching
- **Vault** – secrets (non-dev environments)
- **OpenTelemetry + Jaeger + Grafana** – distributed tracing & diagnostics

📄 Rationale:
- Architecture scope → [ADR-001](./ADRs/ADR-001-Architecture-Showcase-Scope.md)
- Messaging choice → [ADR-006](./ADRs/ADR-006-RabbitMQ-vs-Azure-Service-Bus.md)

---

## Key patterns

### Clean Architecture + DDD

- Explicit domain model with invariants
- Application layer with use-case handlers
- Infrastructure isolated behind interfaces

📄 Decision rationale:
- Overall architecture boundaries → [ADR-001](./ADRs/ADR-001-Architecture-Showcase-Scope.md)

---

### CQRS + Decorator Pipeline

Commands and queries are explicitly separated and processed through a decorator pipeline for:

- Validation
- Transactions
- Observability
- Error handling

📄 Decision rationale:
- CQRS and handler pipeline → [ADR-005](./ADRs/ADR-005-CQRS-and-Decorator-Pipeline.md)

---

### Transactional Outbox

Integration events are written transactionally alongside state changes and published asynchronously to prevent dual-write failures.

📄 Decision rationale:
- Reliable event publishing → [ADR-003](./ADRs/ADR-003-Transactional-Outbox.md)

---

### Strangler-Fig Migration

The system supports **incremental migration** from monolith to microservices by introducing a **Connector API** as a transition layer.

- No big-bang rewrite
- Monolith remains operational
- Capabilities are extracted gradually

📄 Decision rationale:
- Migration strategy → [ADR-001](./ADRs/ADR-001-Architecture-Showcase-Scope.md)

---

### Messaging Strategy

- **RabbitMQ** is used locally for fast iteration and DLQ visibility
- **Azure Service Bus** is the cloud target for guaranteed delivery and operational maturity

📄 Decision rationale:
- RabbitMQ vs Service Bus → [ADR-006](./ADRs/ADR-006-RabbitMQ-vs-Azure-Service-Bus.md)

---

### Dapr (Homelab / Local Only)

Dapr is used **intentionally for homelab and local parity**, not as a production dependency.

It enables:
- Local emulation of cloud primitives
- Config and secret abstraction
- Event-driven experimentation without cloud lock-in

**Target Azure replacements**
- Dapr Config → Azure App Configuration
- Dapr Secrets → Azure Key Vault
- Dapr Pub/Sub → Azure Service Bus
- Dapr Invocation → Azure SignalR (where applicable)

📄 Decision rationale:
- Dapr usage boundaries → [ADR-002](./ADRs/ADR-002-Dapr-Usage-Boundaries.md)

---

### Trace Context Propagation

Trace context is propagated consistently across:
- HTTP calls
- Async messaging
- Background processors

This enables full end-to-end traceability.

📄 Decision rationale:
- Trace propagation → [ADR-007](./ADRs/ADR-007-Trace-Context-Propagation.md)

---

## Observability

The platform is instrumented **end-to-end**:

- Distributed tracing across frontend → APIs → DB → async workflows
- Correlated logs with TraceId
- Practical “find by TraceId” debugging workflow in Grafana

📄 Decision rationale:
- Observability-first design → [ADR-004](./ADRs/ADR-004-Observability-First.md)

See also:
- [`observability.md`](./observability.md)

---

## Health checks

A centralized health dashboard exposes:

- Liveness & readiness
- Dependency health (DB, messaging, config, secrets, external APIs)

📄 Decision rationale:
- Dependency-aware health modeling → [ADR-004](./ADRs/ADR-004-Observability-First.md)

See also:
- [`health-checks.md`](./health-checks.md)

---

## Running locally

The platform is designed to run via containers.

Typical flow:
1. Start infrastructure (SQL, RabbitMQ, Redis, tracing stack)
2. Start monolith and/or microservices
3. Open:
   - Admin UI
   - Jaeger / Grafana
   - Health dashboard

📄 Decision rationale:
- Local vs cloud parity → [ADR-002](./ADRs/ADR-002-Dapr-Usage-Boundaries.md)

---

## Architecture Decision Records

All significant architectural decisions are documented as ADRs:

- [ADR-001 – Architecture Showcase Scope](./ADRs/ADR-001-Architecture-Showcase-Scope.md)
- [ADR-002 – Dapr Usage Boundaries](./ADRs/ADR-002-Dapr-Usage-Boundaries.md)
- [ADR-003 – Transactional Outbox](./ADRs/ADR-003-Transactional-Outbox.md)
- [ADR-004 – Observability First](./ADRs/ADR-004-Observability-First.md)
- [ADR-005 – CQRS and Decorator Pipeline](./ADRs/ADR-005-CQRS-and-Decorator-Pipeline.md)
- [ADR-006 – RabbitMQ vs Azure Service Bus](./ADRs/ADR-006-RabbitMQ-vs-Azure-Service-Bus.md)
- [ADR-007 – Trace Context Propagation](./ADRs/ADR-007-Trace-Context-Propagation.md)

---

## Screenshots

Curated screenshots live under `images/`:

- `images/Observability/`
- `images/StranglerFig/`
- `images/Http/`

See: [`screenshots.md`](./screenshots.md)

---

## Roadmap

- Expand ADR coverage (OData constraints, security boundaries)
- Add C4-style architecture diagrams
- Improve DLQ tooling and replay utilities
- Add IaC (Bicep / Terraform) for Azure portability
- Enhance AI service (streaming, structured output, evaluation)

---

## Notes

This is a **portfolio project** designed to demonstrate architectural thinking, trade-offs, and operational readiness.

The emphasis is on **why decisions were made**, not just implementation details.
