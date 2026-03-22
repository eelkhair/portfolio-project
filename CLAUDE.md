# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A **distributed Job Board platform** demonstrating modern backend architecture with three evolutionary paths side-by-side:
1. **Monolith API** (.NET 9) - Clean Architecture + DDD + CQRS
2. **Microservices** (.NET 9) - Service-per-bounded-context (FastEndpoints)
3. **Strangler-Fig transition layer** via Connector API + Reverse Connector API (bidirectional incremental migration)

Additional services:
- **AI Service v2** (.NET 10) - LLM-powered chat, resume parsing, embedding pipeline (RAG), function calling
- **Gateway** (.NET 9) - YARP reverse proxy routing to all backend services
- **MCP Servers** - Model Context Protocol endpoints for monolith and admin-api

This is a portfolio project designed for Staff/Principal Engineer and Solution Architect evaluation.

## Build and Run Commands

### .NET Services
```bash
dotnet build JobBoard.sln                    # Build entire solution
dotnet run --project services/monolith/Src/Presentation/JobBoard.API/JobBoard.API.csproj
dotnet run --project services/ai-service.v2/Src/Presentation/JobBoard.AI.API/JobBoard.AI.API.csproj
dotnet run --project services/gateway/Gateway.Api.csproj
dotnet run --project services/connector-api/connector-api.csproj
dotnet run --project services/reverse-connector-api/reverse-connector-api.csproj
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

### AI Service v1 (Node.js/TypeScript — legacy)
```bash
cd services/ai-service
npm run dev            # Development with watch (uses dotenv + tsx)
npm run build          # TypeScript compile
```

### Docker Infrastructure
```bash
cd scripts
docker-compose -f docker-compose.dev.yml up -d    # Dev environment
docker-compose -f docker-compose.prod.yml up -d   # Prod environment
```

### Tests
```bash
dotnet test services/monolith/Tests/JobBoard.Monolith.Tests/     # Monolith integration tests (Testcontainers + SQL Server)
dotnet test services/connector-api.tests/                        # Connector API tests
dotnet test services/reverse-connector-api.tests/                # Reverse Connector API tests
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

- **BaseEntity**: `int InternalId` (DB primary key) + `Guid Id` (public API identifier)
- **BaseAuditableEntity**: Adds `CreatedAt/UpdatedAt` (UTC) and `CreatedBy/UpdatedBy`, auto-populated by DbContext
- **Aggregates** use factory methods (`Company.Create(input)`) and domain methods for mutations
- **Value Objects** return `Result<T>` (success/failure) instead of throwing — validation errors accumulate
- `DomainException` is thrown after collecting all validation errors; decorators convert to `ValidationException` for API responses
- Microservices `Elkhair.Dev.Common` BaseEntity uses different naming: `int Id` + `Guid UId`

### API Layer (ASP.NET Core MVC Controllers, not FastEndpoints)

Controllers inherit from `BaseApiController` and use `ExecuteCommandAsync()` to dispatch commands/queries through the decorator pipeline. OData endpoints exist for read-heavy queries.

Key endpoints: `/api/companies`, `/api/jobs`, `/api/drafts`, `/api/odata/*`, `/api/outbox`, `/api/settings`

### AI Service v2 (`services/ai-service.v2/`)

Clean Architecture .NET 10 service replacing the Node.js AI service. Uses FastEndpoints.

**Structure:**
- `Core/JobBoard.AI.Domain` - Domain entities (ResumeEmbedding)
- `Core/JobBoard.AI.Application` - Handlers, tool interfaces, chat options
- `Infrastructure.AI` - LLM provider integration (multi-provider function calling)
- `Infrastructure.Dapr` - Pub/sub handlers, API clients, AI tool implementations
- `Infrastructure.Persistence` - pgvector for embeddings (1536-dim, cosine distance)
- `Infrastructure.Configuration` - Dapr config management
- `Infrastructure.Diagnostics` - OpenTelemetry
- `Infrastructure.HttpClients` - Typed HTTP clients
- `Presentation/JobBoard.AI.API` - FastEndpoints API

**Chat scopes:** `Admin`, `CompanyAdmin`, `Public` — each resolves different tool registries via `IChatOptionsFactory`. Tool registries: `AiToolRegistry` ("ai"), `MonolithToolRegistry` ("monolith"), `AdminToolRegistry` ("micro"), `PublicToolRegistry` ("public"), `CompanyAdminToolRegistry` ("company-admin").

**Endpoints:** `POST /chat` (admin), `POST /chat/company` (company-admin), `POST /chat/public` (applicant)

### Transactional Outbox

`OutboxMessage` stores `EventType`, JSON `Payload`, `TraceParent` (for trace correlation), `RetryCount`, and `LastError`. Background processor fetches TOP 20 unprocessed messages with `UPDLOCK, READPAST` locking, publishes via Dapr to RabbitMQ (`outbox-events` topic), retries up to 3 times before moving to `OutboxDeadLetter`.

- Publisher: `services/monolith/Src/Infrastructure/JobBoard.Infrastructure.Outbox/OutboxPublisher.cs`
- Processor command: `services/monolith/Src/Core/JobBoard.Application/Actions/Outbox/ProcessOutboxMessageCommand.cs` (marked `INoTransaction`)

### Resume Embedding Pipeline (RAG)

Monolith publishes outbox events → RabbitMQ → Dapr pub/sub → AI service handlers:
- `ResumeUploadedV1Event` → parse resume (download blob + extract structured data)
- `ResumeParsedV1Event` → generate embedding, upsert to pgvector
- `ResumeDeletedV1Event` → remove embedding

### Strangler-Fig Connector API

Routes requests between monolith and microservices via typed HTTP clients (`IMonolithClient`, `ICompanyApiClient`, `IJobApiClient`, `IUserApiClient`, `IAdminApiClient`). Implements a `CompanyProvisioningSaga` that: extracts company+admin from monolith → fans out to 3 microservices in parallel (`Task.WhenAll`) → activates in monolith.

### Reverse Connector API (`services/reverse-connector-api/`)

Bidirectional strangler-fig: syncs changes from microservices back to the monolith. Subscribes to Dapr pub/sub events from microservices and propagates state changes to maintain consistency during the migration period.

### Gateway (YARP)

`services/gateway/` — .NET 9 YARP-based reverse proxy that routes traffic to monolith, microservices, connector API, and AI service.

### MCP Servers (Model Context Protocol)

- `services/monolith/Src/Presentation/JobBoard.API.Mcp/` - MCP endpoint for monolith
- `services/micro-services/admin-api/AdminApi.Mcp/` - MCP endpoint for admin API
- `Common/JobBoard.Mcp.Common/` - Shared MCP library

### EF Core

- DbContext: `services/monolith/Src/Infrastructure/JobBoard.Infrastructure.Persistence/Context/JobBoardDbContext.cs`
- Implements `IJobBoardDbContext`, `ITransactionDbContext`, `IOutboxDbContext`
- Entity configs use schema scoping (e.g., `"Companies", "Company"`) with `IEntityTypeConfiguration<T>` in `/Configurations/`
- Each entity has a SQL Server sequence (`{TableName}_Sequence`) for atomic `(Id, InternalId)` pair generation
- `SaveChangesAsync(string userId, ...)` auto-populates audit timestamps

### Shared Libraries

- **`Elkhair.Dev.Common`** — NuGet package for microservices: Dapr extensions, `EventDto<T>`, `ApiResponse<T>`, domain constants, base entities
- **`Elkhair.Common.Observability`** — Serilog + OpenTelemetry tracing (Jaeger OTLP)
- **`Elkhair.Common.Persistence`** — Database migration utilities
- **`JobBoard.Mcp.Common`** — Shared MCP server library
- **`JobBoard.IntegrationEvents`** — Shared event contracts (`ResumeUploadedV1Event`, `ResumeParsedV1Event`, `ResumeDeletedV1Event`, `JobCreatedV1Event`), published to `https://nuget.eelkhair.net/`
- **`JobBoard.Contracts`** — Monolith contracts (ProjectReference to IntegrationEvents)

### Monolith Infrastructure Modules

All in `services/monolith/Src/Infrastructure/`:
- `JobBoard.Infrastructure.Persistence` - EF Core + SQL Server
- `JobBoard.Infrastructure.Outbox` - Transactional outbox publisher
- `JobBoard.Infrastructure.BlobStorage` - Azure Blob Storage
- `JobBoard.Infrastructure.Vault` - Secret management
- `JobBoard.Infrastructure.RedisConfig` - Redis configuration
- `JobBoard.Infrastructure.Messaging` - Message bus
- `JobBoard.Infrastructure.HttpClients` - Typed HTTP clients

### Microservices (`services/micro-services/`)

Four bounded-context services using FastEndpoints (all `AllowAnonymous()`, auth at gateway):
- `company-api/` - Company management
- `job-api/` - Job listings
- `user-api/` - User provisioning (Keycloak group creation)
- `admin-api/` - Admin operations + `AdminApi.Core`, `AdminApi.Contracts`, `AdminApi.Mcp`

### Dapr Integration

- Local components in `/Components/` (YAML): `rabbitmq.yaml` (pub/sub), `statestore.yaml` (Redis state), `config.redis.yaml` (configuration), `jaeger.yaml` (tracing)
- Config keys: `jobboard:config:global:*` and `jobboard:config:{service-name}:*`
- Feature flags propagated via Dapr config subscriber → SignalR to Angular admin
- Azure production replacements: App Configuration, Key Vault, Event Grid/Service Bus

### Authentication (Keycloak)

- **Server:** `https://auth.eelkhair.net` — realms: `job-board` (prod), `job-board-dev` (dev), `job-board-local` (local)
- **Group hierarchy:** `/Admins`, `/Companies/{companyUId}/CompanyAdmins`, `/Companies/{companyUId}/Recruiters`, `/Applicants`
- **JWT claims:** `given_name`, `family_name`, `email`, `groups` (full group path enabled, leading `/` stripped at runtime)
- **Auth policies (AI service):** `AdminChat`, `CompanyAdminChat`, `PublicChat`, `DaprInternal`
- **Admin app:** `angular-auth-oidc-client` with PKCE; **Public app:** `@auth0/auth0-angular` with SSR guards
- **User provisioning:** user-api creates Keycloak groups + sends verification email

### Angular Admin App (`apps/job-admin/`)

Standalone components with lazy-loaded feature routes (Jobs, Companies, Applications, Access Control, Settings, System). Key cross-cutting interceptors: `TraceInterceptor` (OpenTelemetry propagation), `AuthInterceptor` (Keycloak tokens), `IdempotencyInterceptor` (request dedup). Uses PrimeNG components, SignalR for real-time notifications, and an AI Chat component.

### Angular Public App (`apps/job-public/`)

Angular 21 + SSR + Tailwind v4. Job-seeker facing UI with Auth0 authentication (SSR-safe via optional inject pattern). Uses `postcss.config.json` for Tailwind v4 (not `.mjs`). `@apply` does not work in Angular's build pipeline — use plain CSS in `@layer components {}`.

### Observability

Serilog structured logging + OpenTelemetry tracing exported to Jaeger (OTLP). Activity tags capture user ID, email, command/query names. Outbox processor restores parent trace context from stored `TraceParent`. Angular propagates trace IDs via HTTP headers.

## Architecture Decision Records

All significant decisions documented in `docs/ADRs/`:
- ADR-001: Architecture Showcase Scope
- ADR-002: Dapr Usage Boundaries (local only, Azure replacements in cloud)
- ADR-003: Transactional Outbox
- ADR-004: Observability First
- ADR-005: CQRS and Decorator Pipeline
- ADR-006: RabbitMQ vs Azure Service Bus
- ADR-007: Trace Context Propagation
- ADR-008: YARP Gateway Direct Proxy
- ADR-009: AI Service Multi-Provider Function Calling
- ADR-010: Real-Time AI Notifications SignalR
- ADR-011: Strangler-Fig Connector API Provisioning Sagas
- ADR-012: Reverse Connector Bidirectional Strangler-Fig
- ADR-013: Keycloak Migration Auth Strategy
- ADR-014: Resume Embedding Pipeline (RAG)
- ADR-015: MCP Server Integration
- ADR-016: Multi-Scope AI Chat
- ADR-017: IntegrationEvents Shared NuGet Package
- ADR-018: Docker Deployment Topology
- ADR-019: EF Core Dual-ID Sequence Generation

## Debugging Workflow

Use TraceId-driven debugging: search logs in Grafana by TraceId, view distributed traces in Jaeger. TraceId propagates through all synchronous and asynchronous operations.
