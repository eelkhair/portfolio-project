# ADR-020: Aspire Local Orchestration

- **Status:** Accepted
- **Date:** 2026-03-24

## Context

The local development environment previously required manually launching 10+ .NET services, 8 Dapr sidecars, Docker Compose for infrastructure (SQL Server, Postgres, Redis, RabbitMQ, Keycloak), and separate Angular dev servers. Developers needed deep knowledge of service dependencies, port assignments, and configuration sources to get the full system running.

Docker Compose handled infrastructure but couldn't orchestrate .NET projects with Dapr sidecars. The `tye` tool was considered but is no longer maintained. Manual launch profiles in Rider/VS worked but were fragile and couldn't express dependency ordering.

Three approaches were evaluated:

1. **Docker Compose for everything**: Containerize all services. High fidelity but slow rebuild cycles, no debugger attach, and Dapr sidecar orchestration is complex in Compose.
2. **Manual launch profiles**: Each service has its own `launchSettings.json`. Fast iteration but no dependency ordering, no health checks, and 10+ terminal windows.
3. **.NET Aspire AppHost**: Orchestrates .NET projects natively with Dapr sidecar support, manages container infrastructure, provides a dashboard with logs/traces/health, and supports `WaitFor` dependency ordering.

## Decision

Adopt .NET Aspire as the local orchestration layer. A single `aspire/JobBoard.AppHost` project launches the entire platform (~36 resources) with one command.

### Resource Topology

**Infrastructure (containers with `ContainerLifetime.Persistent`):**
- SQL Server (port 11433) — monolith and microservice databases
- PostgreSQL with pgvector (5432) — AI embeddings
- Redis (6379) — configuration (DB 1), state store (DB 0), AI cache (DB 2)
- RabbitMQ (5672/15672) — async messaging
- Keycloak (9999) — identity provider with realm import
- Elasticsearch (9200) — log aggregation
- Azurite (10000-10002) — blob storage emulator
- Mailpit (8025/1025) — email testing
- Seed Runner (`ghcr.io/eelkhair/seed-runner:1.0`) — single persistent container that seeds Redis, SQL Server, and PostgreSQL in parallel on startup, then idles with an HTTP health endpoint

**Observability:**
- Jaeger (16686) — distributed tracing
- OpenTelemetry Collector (4327) — trace/metric ingestion
- Grafana (3200) — dashboards
- Dapr Dashboard — sidecar monitoring

**Services (10 .NET projects + 8 Dapr sidecars):**
- Monolith API, Monolith MCP, Gateway (core, always started)
- AI Service v2, Admin API, Admin API MCP, Company API, Job API, User API, Connector API, Reverse Connector API (conditional on `USE_DAPR` flag)

**Frontends (2 npm apps):**
- Admin UI (port 4200), Public UI (port 3000)

### Configuration Strategy

Services detect Aspire mode via `ASPIRE_MODE=true` environment variable and switch to a different configuration path:

- **Non-Aspire (else branch):** `AddVaultSecrets("{service-name}")` loads from HashiCorp Vault via Dapr secret store, then `AddRedisConfiguration()` subscribes to Dapr config store.
- **Aspire (if branch):** Configuration comes from three sources:
  1. **Dapr vault** (`DaprComponents/secrets/secrets.json`) — connection strings, Keycloak config, API keys
  2. **Dapr config store** (Redis DB 1) — feature flags, per-service config, seeded by `RedisInit/seed.sh`
  3. **Environment variables** set by the AppHost — service URLs, MCP endpoints, infrastructure addresses

### MCP Server Port Management

MCP servers (monolith-mcp, admin-api-mcp) previously hardcoded their ports (`http://+:3333`, `http://+:3334`). Under Aspire, the DCP (Developer Control Plane) proxy intercepts these ports on loopback, causing connection failures from other services.

Solution: MCP servers skip hardcoded port binding under Aspire mode and let Aspire assign ports. The AI service receives MCP URLs dynamically via environment variables (`McpServer__IntegrationUrl`, `McpServer__MicroUrl`) using `GetEndpoint("http")`.

### Seed Runner

A single persistent container (`ghcr.io/eelkhair/seed-runner:1.0`, built from `SeedRunner/Dockerfile` using Ubuntu 22.04 with redis-tools, postgresql-client, and mssql-tools18) replaces the previous three separate seed containers (redis-seed, sqlserver-seed, postgres-seed). The entrypoint (`SeedRunner/entrypoint.sh`) runs all three seed scripts in parallel:

- **Redis** (`RedisInit/seed.sh`): Populates config keys in DB 1 with `SET ... NX` to preserve runtime changes
- **SQL Server** (`SqlServerInit/seed-sqlserver.sh`): Restores `job-board-monolith` and `job-board` databases from `.bak` backups if tables don't exist
- **PostgreSQL** (`PostgresInit/seed-postgres.sh`): Restores `AiEmbeddings` database from `.dump` backup if tables don't exist

After all seeds complete, the container opens an HTTP health endpoint on port 8080. Services use `.WaitFor(seedRunner)` which blocks until this endpoint responds, eliminating race conditions where services start before databases are seeded.

To rebuild the image: `docker build -t ghcr.io/eelkhair/seed-runner:1.0 ./SeedRunner`

### Dependency Ordering

`WaitFor()` ensures services start only after their dependencies are healthy:
- Seed runner waits for infrastructure (SQL Server, Redis, PostgreSQL)
- All services wait for seed runner (ensures databases are seeded before startup)
- All services wait for RabbitMQ and Keycloak
- AI service waits for MCP servers
- Gateway waits for seed runner and monolith

## Consequences

### Positive

- **Single-command startup**: `dotnet run --project aspire/JobBoard.AppHost` launches 36 resources
- **Dependency ordering**: Services start in correct order with deterministic seeding — no race conditions or hardcoded delays
- **Unified dashboard**: Logs, traces, and health for all resources in one UI
- **Debugger-friendly**: .NET projects run as native processes, not containers
- **Persistent containers**: Infrastructure survives AppHost restarts via `ContainerLifetime.Persistent`
- **Conditional Dapr**: `USE_DAPR` flag allows running core services without Dapr sidecars for faster startup

### Tradeoffs

- **DCP proxy layer**: Aspire's proxy can interfere with non-HTTP protocols (MCP Streamable HTTP, WebSockets). Required workarounds for MCP port management.
- **Config surface area**: Three config sources (env vars, Dapr vault, Redis seed) instead of one. Each new service needs entries in all three.
- **Seed runner image**: The `ghcr.io/eelkhair/seed-runner:1.0` image must be pre-built and pushed to GHCR. The `SeedRunner/Dockerfile` is in the repo for rebuilding. Using `AddDockerfile` instead of `AddContainer` is possible but may timeout on first build.
- **URL values in Redis**: Dapr config store key parsing can mangle URLs containing `://`. Infrastructure URLs (Elasticsearch, Jaeger) should come from env vars or vault, not Redis config.

## Implementation Notes

- AppHost project: `aspire/JobBoard.AppHost/JobBoard.AppHost.csproj`
- Dapr components: `aspire/JobBoard.AppHost/DaprComponents/` (shared, secrets, per-service)
- Seed runner: `aspire/JobBoard.AppHost/SeedRunner/` (Dockerfile + entrypoint)
- Seed scripts: `aspire/JobBoard.AppHost/RedisInit/`, `SqlServerInit/`, `PostgresInit/`
- Docker compose (non-Aspire): `scripts/docker-compose.dev.yml`, `scripts/docker-compose.prod.yml`
