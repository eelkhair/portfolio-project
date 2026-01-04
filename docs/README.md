# Job Board Platform (Portfolio)

A **distributed Job Board platform** built to showcase modern backend architecture, event-driven workflows, and production-grade observability.

This repo demonstrates three evolution paths **side-by-side**:

1. **Monolith API** (Clean Architecture + DDD + CQRS)
2. **Microservices** (service-per-bounded-context)
3. **Strangler-Fig transition layer** via **Connector API** (incremental migration from monolith to services)

It also includes **first-class observability**, **health checks**, and an **operationally-realistic homelab deployment** (reverse proxy + containerized infra).

---

## Table of contents

- [What you can do in the system](#what-you-can-do-in-the-system)
- [Architecture at a glance](#architecture-at-a-glance)
- [Key patterns](#key-patterns)
- [Observability](#observability)
- [Health checks](#health-checks)
- [Running locally](#running-locally)
- [Docs](#docs)
- [Screenshots](#screenshots)
- [Roadmap](#roadmap)
- [Notes](#notes)

---

## What you can do in the system

- Create and manage **companies**, **jobs**, and related workflows
- Provision company users (example: through an external identity provider)
- Trigger async workflows via pub/sub and track them end-to-end
- Generate / rewrite job descriptions via an AI service (optional)

---

## Architecture at a glance

**Frontend**
- **Admin UI** (SPA) calls Admin API and/or Monolith API

**APIs**
- **Monolith API**: the “primary” application for the monolith path
- **Connector API**: the strangler layer; routes/coordinates between monolith and microservices
- **Admin API / Company API / Job API / User API**: microservice path (bounded contexts)

**Infrastructure**
- **SQL Server** for transactional data (monolith + microservices DBs)
- **RabbitMQ** for pub/sub (topics + DLQs)
- **Redis** for state/config (and optional caches)
- **Vault** (or equivalent) for secrets in non-dev deployments
- **OpenTelemetry + Jaeger + Grafana** for tracing & querying by TraceId

> The goal is not just “it works,” but that you can **operate** and **debug** it like a real system.

---

## Key patterns

### Clean Architecture + DDD
- Domain model with clear invariants
- Application layer with use-case handlers (CQRS)
- Infrastructure boundary for persistence/integration

### Transactional Outbox
Reliable event publishing without losing events on crashes / retries.

### Strangler-Fig migration
An incremental migration technique:
- keep the monolith “alive”
- introduce a **Connector API** as a transition layer
- slowly extract capabilities into services without a big-bang rewrite

### Idempotency + Sagas
Async workflows are modeled with explicit steps and guardrails:
- idempotency keys
- retry safety
- DLQ visibility

---

## Observability

This system is instrumented end-to-end:

- **Distributed tracing** across frontend → APIs → DB → async pub/sub
- **Trace correlation in logs**
- A practical **“find by TraceId”** workflow in Grafana

See: **[Observability](./observability.md)**

---

## Health checks

A dedicated health dashboard shows:
- app-level health (liveness/readiness)
- dependency checks (DB, pub/sub, state, config, secret store, external APIs)

See: **[Health checks](./health-checks.md)**

---

## Running locally

This repo is designed to be runnable with containers.

Typical flow:
1. Start infrastructure (SQL, RabbitMQ, Redis, tracing stack)
2. Start services (monolith and/or microservices)
3. Open:
   - Admin UI
   - Jaeger / Grafana
   - Health dashboard

> Exact commands vary by environment; use the compose files / scripts in the repo.  
> If you share your current compose layout, I can align this section to your exact commands.

---

## Docs

- **[Observability](./observability.md)** – traces, logs, and “find by TraceId”
- **[Strangler-Fig Migration](./strangler-fig.md)** – stages, Connector API, safe extraction
- **[Health checks](./health-checks.md)** – how health is modeled and what’s checked
- **[Screenshots](./screenshots.md)** – curated evidence (traces, dashboards, queues, config)

---

## Screenshots

Curated screenshots live under `images/` (grouped by topic):

- `images/Observability/*`
- `images/Strangler Fig/*`
- `images/Http/*`

See: **[Screenshots](./screenshots.md)**

---

## Roadmap
> NOTE: A Docker Compose reference will be added to document the local
> orchestration strategy used for development and testing.
- Expand docs (ADRs, architecture diagrams)
- Harden async workflows (more DLQ tooling, replay utilities)
- Add IaC (Bicep / Terraform) for cloud portability
- Improve AI service (streaming, structured outputs, evals)

---

## Notes

This is a portfolio project; some names/endpoints are simplified.  
If you want this README tailored to the exact repository structure (folders, compose names, scripts), upload the repo tree or paste `ls -R` for the root.
