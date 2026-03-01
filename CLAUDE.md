# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A **distributed Job Board platform** demonstrating modern backend architecture with three evolutionary paths side-by-side:
1. **Monolith API** - Clean Architecture + DDD + CQRS
2. **Microservices** - Service-per-bounded-context
3. **Strangler-Fig transition layer** via Connector API (incremental migration)

This is a portfolio project designed for Staff/Principal Engineer and Solution Architect evaluation.

## Build and Run Commands

### .NET Services
```bash
dotnet build JobBoard.sln                    # Build entire solution
dotnet run --project services/monolith/Src/Presentation/JobBoard.API/JobBoard.API.csproj
```

### EF Core Migrations
```bash
dotnet ef migrations add MigrationName --project services/monolith/Src/Infrastructure/JobBoard.Infrastructure.Persistence/
```

### Frontend - Admin UI (Angular 20)
```bash
cd apps/job-admin
npm start              # Dev server with API proxy
npm run build          # Production build
npm test               # Run Karma tests
```

### Frontend - Public UI (Angular 21 + SSR + Tailwind v4)
```bash
cd apps/job-public
npm start              # Dev server (ng serve)
npm run build          # Production build
npm test               # Run tests
npm run serve:ssr:job-public  # Serve SSR build
```

### AI Service (Node.js/TypeScript)
```bash
cd services/ai-service
npm run dev            # Development with watch (uses dotenv + tsx)
npm run build          # TypeScript compile
npm run debug:brk      # Debug with inspector on port 9229
```

### Infrastructure (via Docker)
```bash
cd services/monolith/Documentation
docker-compose up -d   # Start Azurite, Kafka, Zookeeper, Kafka UI
```

## Architecture

### CQRS + Decorator Pipeline (Custom, not MediatR)

Handlers implement `IHandler<TRequest, TResult>`. Requests extend `BaseCommand<TResponse>` or `BaseQuery<TResult>`.

**Decorator execution order** (outer → inner):
1. `UserContextCommandHandlerDecorator` - Auth check + user sync
2. `ObservabilityCommandHandlerDecorator` - Logging, metrics, OpenTelemetry spans
3. `ValidationCommandHandlerDecorator` - FluentValidation rules
4. `TransactionCommandHandlerDecorator` - DB transaction (skip with `INoTransaction` marker)
5. `ExceptionHandlingCommandHandlerDecorator` - Converts `DomainException` and SQL constraint violations to validation failures
6. **Core handler** - Business logic

Handlers are auto-registered via **Scrutor** scanning for `IHandler<,>`, then decorators are applied with `services.Decorate()`.

Key locations:
- Handler interfaces: `services/monolith/Src/Core/JobBoard.Application/Interfaces/Configurations/`
- Decorators: `services/monolith/Src/Core/JobBoard.Application/Infrastructure/Decorators/`
- Registration: `services/monolith/Src/Core/JobBoard.Application/DependencyInjection.cs`

### Domain Layer Conventions

- **BaseEntity**: `int Id` (DB primary key) + `Guid InternalId` (public API identifier)
- **BaseAuditableEntity**: Adds `CreatedAt/UpdatedAt` (UTC) and `CreatedBy/UpdatedBy`, auto-populated by DbContext
- **Aggregates** use factory methods (`Company.Create(input)`) and domain methods for mutations
- **Value Objects** return `Result<T>` (success/failure) instead of throwing — validation errors accumulate
- `DomainException` is thrown after collecting all validation errors; decorators convert to `ValidationException` for API responses

### API Layer (ASP.NET Core MVC Controllers, not FastEndpoints)

Controllers inherit from `BaseApiController` and use `ExecuteCommandAsync()` to dispatch commands/queries through the decorator pipeline. OData endpoints exist for read-heavy queries.

Key endpoints: `/api/companies`, `/api/jobs`, `/api/drafts`, `/api/odata/*`, `/api/outbox`, `/api/settings`

### Transactional Outbox

`OutboxMessage` stores `EventType`, JSON `Payload`, `TraceParent` (for trace correlation), `RetryCount`, and `LastError`. Background processor fetches TOP 20 unprocessed messages with `UPDLOCK, READPAST` locking, publishes via Dapr to RabbitMQ (`outbox-events` topic), retries up to 3 times before moving to `OutboxDeadLetter`.

- Publisher: `services/monolith/Src/Infrastructure/JobBoard.Infrastructure.Outbox/OutboxPublisher.cs`
- Processor command: `services/monolith/Src/Core/JobBoard.Application/Actions/Outbox/ProcessOutboxMessageCommand.cs` (marked `INoTransaction`)

### Strangler-Fig Connector API

Routes requests between monolith and microservices via typed HTTP clients (`IMonolithClient`, `ICompanyApiClient`, `IJobApiClient`, `IUserApiClient`, `IAdminApiClient`). Implements a `CompanyProvisioningSaga` that: extracts company+admin from monolith → fans out to 3 microservices in parallel (`Task.WhenAll`) → activates in monolith.

### EF Core

- DbContext: `services/monolith/Src/Infrastructure/JobBoard.Infrastructure.Persistence/Context/JobBoardDbContext.cs`
- Implements `IJobBoardDbContext`, `ITransactionDbContext`, `IOutboxDbContext`
- Entity configs use schema scoping (e.g., `"Companies", "Company"`) with `IEntityTypeConfiguration<T>` in `/Configurations/`
- Each entity has a SQL Server sequence (`{TableName}_Sequence`) for atomic `(Id, InternalId)` pair generation
- `SaveChangesAsync(string userId, ...)` auto-populates audit timestamps

### Microservices Shared Library (`Elkhair.Dev.Common`)

NuGet package (v1.0.18, .NET 8) providing: Dapr extensions (HTTP client creation, pub/sub `MessageSender`, state `StateManager`), `EventDto<T>` envelope, `ApiResponse<T>` wrapper, domain constants (`EventTypes`, `PubSubNames`, `TopicNames`, `RecordStatuses`), and `BaseEntity`/`BaseAuditableEntity` base classes.

### Dapr Integration

- Local components in `/Components/` (YAML): `rabbitmq.yaml` (pub/sub), `statestore.yaml` (Redis state), `config.redis.yaml` (configuration), `jaeger.yaml` (tracing)
- Config keys: `jobboard:config:global:*` and `jobboard:config:{service-name}:*`
- Feature flags propagated via Dapr config subscriber → SignalR to Angular admin
- Azure production replacements: App Configuration, Key Vault, Event Grid/Service Bus

### Angular Admin App (`apps/job-admin/`)

Standalone components with lazy-loaded feature routes (Jobs, Companies, Applications, Access Control, Settings, System). Key cross-cutting interceptors: `TraceInterceptor` (OpenTelemetry propagation), `AuthInterceptor` (Auth0 tokens), `IdempotencyInterceptor` (request dedup). Uses PrimeNG components, SignalR for real-time notifications, and an AI Chat component.

### Observability

Shared library: `Common/Elkhair.Common.Observability/` (v1.0.3). Serilog structured logging + OpenTelemetry tracing exported to Jaeger (OTLP). Activity tags capture user ID, email, command/query names. Outbox processor restores parent trace context from stored `TraceParent`. Angular propagates trace IDs via HTTP headers.

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
