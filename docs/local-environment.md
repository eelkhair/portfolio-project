## Local Development Environment

This project includes a deliberately designed **local development environment**
that prioritizes developer experience, portability, and architectural clarity.

The local setup allows the entire system to run without cloud dependencies while
preserving the same architectural patterns used in production.

### Local Environment Goals

- Single-command startup for the entire platform
- Fast local iteration with debugger support
- Full-system observability (traces, logs, metrics)
- Infrastructure replaceability
- Clear mapping to Azure production services

### Running Locally

#### Option 1: Aspire (Recommended)

Launches all 36 resources (10 services, 8 Dapr sidecars, 2 frontends, infrastructure, observability) with one command:

```bash
dotnet run --project aspire/JobBoard.AppHost/JobBoard.AppHost.csproj
```

The Aspire dashboard at `http://localhost:15888` provides unified logs, traces, and health for all resources.

See [ADR-020: Aspire Local Orchestration](ADRs/ADR-020-Aspire-Local-Orchestration.md) for architecture details.

#### Prerequisites

1. **Docker Desktop** — required for infrastructure containers
2. **Keycloak realm** — local realm `job-board-local` with:
   - Audience mapper (`jobboard-api`)
   - Groups mapper (Full group path ON)
   - Service client (`dapr-service-client`) with realm-management roles
3. **AI Service API keys** — stored in .NET User Secrets (not committed):
   ```bash
   cd services/ai-service.v2/Src/Presentation/JobBoard.AI.API
   dotnet user-secrets set "AI:OPENAI_API_KEY" "sk-..."
   dotnet user-secrets set "AI:OPENAI_MODEL" "gpt-4.1-mini"
   dotnet user-secrets set "AI:AZURE_API_KEY" "your-azure-openai-key"
   dotnet user-secrets set "AI:AZURE_API_Endpoint" "https://your-resource.openai.azure.com"
   dotnet user-secrets set "AI:AZURE_OPENAI_MODEL" "gpt-4o-mini"
   dotnet user-secrets set "AI:CLAUDE_API_KEY" "sk-ant-..."
   dotnet user-secrets set "AI:GEMINI_API_KEY" "your-gemini-key"
   dotnet user-secrets set "AI:GEMINI_MODEL" "gemini-2.0-flash-lite"
   ```

#### Option 2: Docker Compose (Infrastructure Only)

For running services individually outside Aspire:

```bash
cd scripts
docker-compose -f docker-compose.dev.yml up -d
```

Then launch individual services via Rider/VS or `dotnet run`.

### Configuration Sources

Under Aspire, services receive configuration from three sources (in priority order):

| Source | What it provides | Examples |
|--------|-----------------|----------|
| Environment variables (AppHost) | Service URLs, MCP endpoints, infrastructure addresses | `MonolithUrl`, `McpServer__IntegrationUrl` |
| Dapr vault (secrets.json) | Connection strings, Keycloak config, API keys | `ConnectionStrings:Monolith`, `Keycloak:Authority` |
| Redis config (DB 1, seeded) | Feature flags, per-service config | `FeatureFlags:Monolith`, `AIProvider` |

Feature flags and AI provider/model use `SET ... NX` in the seed script, so runtime changes persist across Aspire restarts.

### Local vs Azure Mapping

| Capability | Local Environment | Azure Target |
|----------|------------------|-------------|
| Configuration | Dapr + Redis | Azure App Configuration |
| Secrets | Dapr + Vault (file) | Azure Key Vault |
| Messaging | RabbitMQ | Azure Service Bus |
| Eventing | Dapr Pub/Sub | Azure Event Grid |
| Real-time Updates | SignalR (self-hosted) | Azure SignalR |
| Blob Storage | Azurite | Azure Blob Storage |
| Identity | Keycloak | Keycloak (self-hosted on Azure) |
| Tracing | Jaeger + OTel Collector | Azure Monitor / Application Insights |
| Logs | Elasticsearch + Grafana | Azure Monitor Logs |
| Email | Mailpit | SendGrid / Azure Communication Services |
| Orchestration | .NET Aspire | App Service / Container Apps |
| Hosting | Docker (local) | Docker (Azure Container Apps) |

### Important Notes

- Redis and Vault are used **only for local development**.
- Dapr is used to simplify local integration and experimentation (see [ADR-002](ADRs/ADR-002-Dapr-Usage-Boundaries.md)).
- All infrastructure dependencies are abstracted behind application interfaces
  to ensure clean replacement in Azure deployments.
- `ASPIRE_MODE=true` environment variable switches services to the Aspire configuration path.
- MCP server ports are managed by Aspire's DCP proxy; services receive URLs dynamically.
- The `USE_DAPR` flag in AppHost allows running core services without Dapr sidecars for faster startup.
