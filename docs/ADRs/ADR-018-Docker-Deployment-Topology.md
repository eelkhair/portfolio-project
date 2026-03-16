# ADR-018: Docker Deployment Topology

- **Status:** Accepted
- **Date:** 2026-03-16

## Context

The platform consists of 10+ services, each potentially requiring a Dapr sidecar, plus infrastructure components (message broker, dashboards, logging). Deploying this on a single Linux host for dev/staging requires a topology that balances operational simplicity with the distributed architecture the project demonstrates.

Key constraints:
- **Single host**: Dev and staging environments run on one Linux server. Kubernetes is not used — the goal is to demonstrate distributed patterns without the operational overhead of a cluster.
- **Dapr sidecars**: Each Dapr-enabled service needs a co-located sidecar container with access to shared and service-specific component YAML files.
- **Environment parity**: Dev and prod compose files should be structurally identical, differing only in environment variables and health check URLs.
- **Health visibility**: All services must be monitorable from a single dashboard.

## Decision

### Docker Compose with Service + Sidecar Pattern

Each Dapr-enabled service is deployed as a pair of containers:

```
service:
  image: registry.eelkhair.net/{service}:latest
  ports: ["{host-port}:8080"]
  environment: [ASPNETCORE_ENVIRONMENT=...]
  networks: [ai-job-board-net]

service-sidecar:
  image: daprio/daprd:1.15.8
  depends_on: [service]
  network_mode: "service:{service}"
  volumes:
    - /home/eelkhair/Dapr/shared/Components:/Components
    - /home/eelkhair/Dapr/{service}/:/Dapr
  command: [daprd, --app-id, {service}, --app-port, 8080, ...]
```

The sidecar uses `network_mode: "service:{service}"` to share the service's network namespace — the sidecar and app communicate over `localhost`, matching Dapr's expected model.

### Dapr Component Volume Strategy

Component YAML files are organized on the host filesystem:

```
/home/eelkhair/Dapr/
  shared/
    Components/          → Shared components (pub/sub, state store, secrets)
    Config/              → Shared Dapr config (tracing, middleware)
  {service}/
    Components/          → Service-specific components
    Components/Secrets/  → Service-specific secret stores
    Components/Timers/   → Service-specific cron bindings
    Config/
      config.yaml        → Service-specific Dapr configuration
```

Each sidecar mounts both shared and service-specific component directories via `--resources-path`. This allows:
- **Shared infrastructure** (RabbitMQ pub/sub, Redis state, Vault secrets) to be defined once
- **Service-specific overrides** (subscriptions, timers, secret scopes) to be isolated per service
- **Configuration changes** without rebuilding images — edit YAML, restart the sidecar

### Service Ordering in Compose Files

Services are organized by architectural layer for readability and operational clarity:

1. **Frontend**: `job-admin`, `job-public`
2. **Gateway**: YARP reverse proxy
3. **AI Services**: `ai-service` (Node.js), `ai-service-v2` (.NET)
4. **Monolith**: `monolith-api`, `monolith-mcp`
5. **Microservices**: `admin-api` + MCP, `company-api`, `job-api`, `user-api`
6. **Connectors**: `connector-api`, `reverse-connector-api`
7. **Infrastructure**: `dapr-dashboard`, `job-board-status` (health checks), `seq` (logging)

This ordering mirrors the health check dashboard layout, making it easy to correlate compose file entries with monitoring.

### Health Check Dashboard

A `job-board-status` container runs the HealthChecks UI, configured entirely via environment variables:

```
HealthChecksUI__HealthChecks__{index}__Name={display name}
HealthChecksUI__HealthChecks__{index}__Uri={healthz URL}
```

Health checks are ordered to match the service layers: Gateway → AI → Monolith → Microservices → Connectors. Each service exposes `/healthzEndpoint` and `/liveness` endpoints.

### Two Compose Files (Dev and Prod)

| Aspect | Dev (`docker-compose.dev.yml`) | Prod (`docker-compose.prod.yml`) |
|--------|-------------------------------|----------------------------------|
| Environment | `ASPNETCORE_ENVIRONMENT=dev` (monolith, gateway) | `ASPNETCORE_ENVIRONMENT=Production` |
| Health check URLs | `*-dev.eelkhair.net` | `*.eelkhair.net` |
| Vault/Config | Dev Vault tokens | Prod Vault tokens |
| Structure | Identical | Identical |

Both files are structurally identical — same services, same ports, same volume mounts. Only environment variables and health check URLs differ. This makes it straightforward to promote from dev to prod.

### Private Container Registry

All service images are pushed to `registry.eelkhair.net/{service}:latest`. A separate publish script (`scripts/docker-publsh.sh`) builds and pushes images for all services. The registry is self-hosted alongside the deployment infrastructure.

### Networking

All services join a single Docker bridge network (`ai-job-board-net`). Services reference each other by Docker DNS names (e.g., `http://monolith-api:8080`, `http://monolith-mcp:3333`). The Dapr sidecars communicate with each other over this network for service invocation and pub/sub.

## Consequences

### Positive

- **Operational simplicity**: A single `docker compose up -d` starts the entire platform. No Kubernetes manifests, Helm charts, or service mesh configuration.
- **Dapr-native**: The sidecar pattern in Docker Compose mirrors how Dapr runs in Kubernetes (sidecar injection), making the architecture transferable.
- **Environment parity**: Dev and prod are structurally identical, reducing "works on dev" surprises.
- **Observable**: The health check dashboard, Dapr dashboard, and Seq logging provide full visibility from a single browser.
- **Component hot-reload**: Dapr component YAML changes take effect on sidecar restart without rebuilding application images.

### Tradeoffs

- **No orchestration**: Docker Compose provides no rolling updates, auto-scaling, or self-healing. A crashed container stays down until manually restarted (mitigated by `restart: unless-stopped` on frontend services).
- **Single host ceiling**: All services share one machine's resources. CPU/memory contention is possible under load. Acceptable for portfolio demonstration.
- **Volume management**: Component YAML files live on the host filesystem, not in version control (they contain environment-specific paths and may reference local secrets). The directory structure must be manually replicated when setting up a new host.
- **Port management**: Each service maps to a unique host port (5200, 5238, 5280, 5284, 5285, 6062, 6072, 6080, 6081, 6082, 6084, 6092, etc.). The port assignments are documented only in the compose files.

## Implementation Notes

- **Dapr version**: All sidecars pin `daprio/daprd:1.15.8` for consistency.
- **Infrastructure services**: Dapr Dashboard, Seq, and the health check UI do not have Dapr sidecars — they're pure infrastructure.
- **The `ai-service` (Node.js v1)** runs on port 6082 with `NODE_ENV=prod` and has a Dapr sidecar for pub/sub and config access.
- **MCP servers** (`monolith-mcp:3333`, `admin-api-mcp:3334`) run on non-standard ports to avoid conflicts with their parent APIs.
- **Vault integration**: The monolith and gateway inject `VAULT_ADDR` and `VAULT_TOKEN` via environment variables (sourced from `.env` files). Microservices access Vault through Dapr's secret store component.
