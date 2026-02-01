# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A **distributed Job Board platform** demonstrating modern backend architecture with three evolutionary paths side-by-side:
1. **Monolith API** - Clean Architecture + DDD + CQRS
2. **Microservices** - Service-per-bounded-context
3. **Strangler-Fig transition layer** via Connector API (incremental migration)

This is a portfolio project designed for Staff/Principal Engineer and Solution Architect evaluation.

## Build and Run Commands

### .NET Services (monolith, microservices, connector-api)
```bash
dotnet build JobBoard.sln          # Build entire solution
dotnet run --project services/monolith/Src/Presentation/JobBoard.API/JobBoard.API.csproj
```

### Frontend - Admin UI (Angular 20)
```bash
cd apps/job-admin
npm install
npm start              # Dev server with API proxy
npm run build          # Production build
npm test               # Run Karma tests
```

### Frontend - Public UI (Next.js 15)
```bash
cd apps/job-public
npm install
npm run dev            # Dev server with Turbopack
npm run build          # Production build
npm run lint           # ESLint
```

### AI Service (Node.js/TypeScript)
```bash
cd services/ai-service
npm install
npm run dev            # Development with watch
npm run debug:brk      # Debug with inspector on port 9229
```

### Infrastructure (via Docker)
```bash
cd services/monolith/Documentation
docker-compose up -d   # Start SQL Server, RabbitMQ, Redis, tracing stack
```

## Architecture

### Directory Structure
```
apps/
  job-admin/           # Angular 20 admin UI (Auth0, SignalR, OpenTelemetry)
  job-public/          # Next.js 15 public job board

services/
  monolith/            # Clean Architecture monolith
    Src/Core/          # Domain + Application layers
    Src/Infrastructure/# EF Core, Dapr, Outbox, Diagnostics
    Src/Presentation/  # API layer (FastEndpoints, OData)

  micro-services/
    admin-api/         # Admin bounded context
    company-api/       # Company bounded context
    job-api/           # Job bounded context
    user-api/          # User bounded context
    Elkhair.Dev.Common/# Shared library for microservices
    HealthChecks/      # Central health dashboard

  connector-api/       # Strangler-Fig transition layer
  ai-service/          # AI integration (Fastify + OpenAI)
  ai-service.v2/       # AI service v2 (.NET 9)

docs/
  ADRs/                # Architecture Decision Records
  observability.md     # Tracing/logging workflow
  health-checks.md     # Health monitoring strategy
```

### Key Patterns

**Clean Architecture** - Strict dependency flow: Domain → Application → Infrastructure → Presentation

**CQRS + Decorator Pipeline** - Commands and queries processed through decorators for validation, transactions, observability, and error handling. Custom implementation (not MediatR-locked).

**Transactional Outbox** - Integration events written atomically with state changes, published asynchronously to prevent dual-write failures.

**Strangler-Fig Migration** - Connector API routes requests between monolith and microservices, enabling incremental extraction without big-bang rewrites.

**Trace Context Propagation** - TraceId flows through HTTP calls, async messaging, and background processors for end-to-end traceability.

### Technology Stack

- **.NET 9** / ASP.NET Core / EF Core 9
- **FastEndpoints** for HTTP endpoints
- **Dapr** for local cloud primitive emulation (config, secrets, pub/sub)
- **RabbitMQ** locally, **Azure Service Bus** in cloud
- **OpenTelemetry** → Jaeger → Grafana for observability
- **Angular 20** with PrimeNG, **Next.js 15** with React 19
- **SQL Server** for persistence

### DTOs and API Design

DTOs intentionally hide database primary keys. The API uses public identifiers to prevent over-exposure of internal data models. OData is available for read-heavy scenarios with safeguards for query depth and operation restrictions.

## Architecture Decision Records

All significant decisions documented in `docs/ADRs/`:
- ADR-001: Architecture Showcase Scope
- ADR-002: Dapr Usage Boundaries (local only, Azure replacements in cloud)
- ADR-003: Transactional Outbox
- ADR-004: Observability First
- ADR-005: CQRS and Decorator Pipeline
- ADR-006: RabbitMQ vs Azure Service Bus
- ADR-007: Trace Context Propagation

## Debugging Workflow

Use TraceId-driven debugging: search logs in Grafana by TraceId, view distributed traces in Jaeger. TraceId propagates through all synchronous and asynchronous operations.
