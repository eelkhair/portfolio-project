# Aspire Two-Stack Split: Infrastructure + Services

## Overview

Split the single AppHost into two independent Aspire AppHost projects:
1. **JobBoard.Infrastructure.AppHost** — persistent infra containers (start once)
2. **JobBoard.AppHost** — application services as Docker containers (rebuild/restart freely)

Services switch from `AddProject<>()` to `AddDockerfile()` so they run as containers built from existing Dockerfiles (same contexts as `docker-publsh.sh`).

---

## Stack 1: `aspire/JobBoard.Infrastructure.AppHost/`

**New project.** Contains all infrastructure containers (moved from current AppHost):

| Resource | Image | Host Port(s) |
|---|---|---|
| sqlserver | mcr.microsoft.com/mssql/server:2022-latest | 11433 → 1433 |
| postgres | ankane/pgvector:latest | 5432 → 5432 |
| redis | redis:8.2 | 6379 → 6379 |
| redis-commander | rediscommander/redis-commander | 8081 → 8081 |
| rabbitmq | rabbitmq:4.2-management | 5672, 15672 |
| keycloak | Aspire Keycloak integration | 9999 |
| jaeger | jaegertracing/all-in-one | 16686, 4317, 9411 |
| mailpit | axllent/mailpit | 8025, 1025 |

All containers use `WithLifetime(ContainerLifetime.Persistent)` and `isProxied: false`.

**Files:**
- `aspire/JobBoard.Infrastructure.AppHost/JobBoard.Infrastructure.AppHost.csproj`
- `aspire/JobBoard.Infrastructure.AppHost/Program.cs`
- Copy `KeycloakRealm/` folder from existing AppHost

---

## Stack 2: `aspire/JobBoard.AppHost/` (refactored)

Replace all `AddProject<>()` with `AddDockerfile()`. Remove ProjectReferences from csproj.

### Service → Dockerfile Mapping

| Resource | Context (from AppHost) | Dockerfile (from context) | Port |
|---|---|---|---|
| monolith-api | `../../services/monolith` | `Src/Presentation/JobBoard.API/Dockerfile` | 5280→8080 |
| monolith-mcp | `../../services/monolith` | `Src/Presentation/JobBoard.API.Mcp/Dockerfile` | 3333→3333 |
| ai-service-v2 | `../../services/ai-service.v2` | `Src/Presentation/JobBoard.AI.API/Dockerfile` | 5200→8080 |
| gateway | `../../services/gateway` | (default) | 5238→8080 |
| admin-api | `../../services/micro-services/admin-api` | (default) | 5262→8080 |
| admin-api-mcp | `../../services/micro-services/admin-api` | `AdminApi.Mcp/Dockerfile` | 3334→3334 |
| company-api | `../../services/micro-services/company-api` | (default) | 5272→8080 |
| job-api | `../../services/micro-services/job-api` | (default) | 5282→8080 |
| user-api | `../../services/micro-services/user-api` | (default) | 5292→8080 |
| connector-api | `../../services/connector-api` | (default) | 5284→8080 |
| reverse-connector-api | `../../services/reverse-connector-api` | (default) | 5285→8080 |

All service containers get `ASPNETCORE_ENVIRONMENT=Development`.

### Dapr Sidecars as Containers

For each Dapr-dependent service, add a sidecar container using `daprio/daprd:1.15.8`. The sidecar and its service communicate via the Aspire Docker network (not shared network namespace). This means:

- **Sidecar → App**: uses `--app-channel-address <service-resource-name>` (Docker network hostname)
- **App → Sidecar**: set `DAPR_HTTP_ENDPOINT=http://<sidecar-resource-name>:3500` and `DAPR_GRPC_ENDPOINT=http://<sidecar-resource-name>:50001` on the service container

**Dapr-dependent services** (7 total):
- ai-service-v2 (extra: `DaprComponents/ai-service-v2/`)
- admin-api
- company-api
- job-api
- user-api (extra: `DaprComponents/user-api/`)
- connector-api
- reverse-connector-api

**Not Dapr-dependent** (keep as-is, no sidecar):
- monolith-api, monolith-mcp, gateway

Each sidecar container bind-mounts DaprComponents and runs daprd with appropriate args.

### Dapr Component Files — Containerized Variants

Create `DaprComponents/container/` with copies of shared YAML updated for container networking:

- `rabbitmq.yaml`: `host.docker.internal:5672` (infra is in the other AppHost, exposed on host)
- `statestore.yaml`: `host.docker.internal:6379`
- `config.yaml`: `host.docker.internal:4317` for OTLP tracing
- `secret-store.yaml`: path updated to `/Secrets/secrets.json` (bind-mount path)
- `secrets.json`: all `127.0.0.1`/`localhost` → `host.docker.internal`

Service-specific components (cron bindings) don't need changes — they're self-contained.

### Health Checks Container

Update URLs from `host.docker.internal:<port>` to container hostnames:
```
http://gateway:8080/healthzEndpoint
http://monolith-api:8080/healthzEndpoint
http://monolith-mcp:3333/healthzEndpoint
...
```
All containers are on the same Aspire Docker network, so hostname resolution works.

### Frontend Apps

Keep as `AddNpmApp` (no Dockerfile needed for dev — Angular dev server with hot reload).

### Dapr Dashboard

Keep as `AddExecutable("dapr-dashboard", "dapr", ".", "dashboard", "-p", "8888")`.

---

## Csproj Changes

**Infrastructure AppHost csproj:**
- SDK: `Aspire.AppHost.Sdk/13.1`
- Packages: `Aspire.Hosting.Keycloak`
- No ProjectReferences
- Content: `KeycloakRealm/**/*`

**Services AppHost csproj:**
- Remove ALL `<ProjectReference>` items (no longer using AddProject)
- Remove `Aspire.Hosting.SqlServer`, `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Redis`, `Aspire.Hosting.RabbitMQ`, `Aspire.Hosting.Keycloak` packages (infra moved out)
- Keep: `Aspire.Hosting.NodeJs` (for AddNpmApp), `CommunityToolkit.Aspire.Hosting.Dapr` (remove if not needed — we're using manual sidecar containers now)
- Content: `DaprComponents/**/*`

---

## Solution File

Add `aspire/JobBoard.Infrastructure.AppHost/JobBoard.Infrastructure.AppHost.csproj` to `JobBoard.sln`.

---

## Execution Steps

1. Create Infrastructure AppHost project (csproj + Program.cs + KeycloakRealm)
2. Create `DaprComponents/container/` with updated YAML/JSON files
3. Refactor Services AppHost Program.cs (AddDockerfile + sidecar containers + health checks)
4. Update Services AppHost csproj (remove ProjectReferences, trim packages)
5. Add Infrastructure AppHost to solution
