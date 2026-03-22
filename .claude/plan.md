# Aspire AppHost Implementation Plan

## Goal
Add .NET Aspire as a **local dev orchestrator** so evaluators can clone the repo, run `dotnet run --project AppHost`, and have the entire distributed system running locally — without access to Proxmox infrastructure.

**Your existing workflow is untouched.** Rider compound config, docker-compose files, Proxmox Dapr components all remain as-is.

---

## Configuration Strategy: appsettings as local defaults

Currently:
- `appsettings.json` has connection strings pointing to Proxmox (`sqlserver.eelkhair.net`, `192.168.1.160`)
- All other config (service URLs, feature flags, OTEL, SMTP) comes from Redis via Dapr at runtime
- Secrets come from HashiCorp Vault via Dapr

For Aspire:
- **Connection strings** → Aspire injects via `WithReference()` as env vars (override appsettings)
- **App config** (service URLs, feature flags, etc.) → Add to `appsettings.json` as defaults. When running via Proxmox, Dapr Redis config overrides them at runtime. When running via Aspire, no Redis config component is loaded so appsettings values are used directly
- **Secrets** (Keycloak credentials, etc.) → Dapr `secretstores.local.file` component with a JSON file of dev-safe values
- **No Redis config seeding needed** — appsettings provides the baseline

This means: services boot from appsettings → Aspire overrides connection strings → Dapr provides pub/sub + state store. No Redis config store component in Aspire Dapr YAMLs at all.

---

## Phase 1: AppHost Project Setup

### 1.1 Create the AppHost project
- New project: `aspire/JobBoard.AppHost/JobBoard.AppHost.csproj`
- Target: `net9.0`
- NuGet packages:
  - `Aspire.Hosting.AppHost`
  - `Aspire.Hosting.Dapr`
  - `Aspire.Hosting.SqlServer`
  - `Aspire.Hosting.PostgreSQL`
  - `Aspire.Hosting.Redis`
  - `Aspire.Hosting.RabbitMQ`
  - `Aspire.Hosting.Keycloak` (community / CommunityToolkit)
- ProjectReferences to all service .csproj files
- Add to `JobBoard.sln`

### 1.2 ServiceDefaults (skip for v1)
- Not wired into existing services — can adopt incrementally later
- Services already have their own OpenTelemetry + health check setup

---

## Phase 2: Infrastructure Containers

### 2.1 `Program.cs` — Container resources

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// --- Infrastructure ---
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .AddDatabase("JobBoard");

var postgres = builder.AddPostgres("postgres")
    .WithImage("ankane/pgvector")  // pgvector support
    .WithDataVolume()
    .AddDatabase("AiEmbeddings");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume()
    .WithRealmImport("./KeycloakRealm/");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithEndpoint(16686, 16686, name: "ui")
    .WithEndpoint(4317, 4317, name: "otlp-grpc")
    .WithEndpoint(9411, 9411, name: "zipkin");
```

### 2.2 Keycloak realm export (manual step — you)
- Export `job-board-local` realm from your Keycloak
- Place in `aspire/JobBoard.AppHost/KeycloakRealm/job-board-local.json`
- Includes: clients, groups hierarchy, realm settings, email theme reference
- No real user data

### 2.3 pgvector init
- Using `ankane/pgvector` image which has the extension pre-installed
- AI service EF Core migrations handle table creation including vector columns

---

## Phase 3: Aspire-Local Dapr Components

### 3.1 Directory structure

```
aspire/JobBoard.AppHost/DaprComponents/
├── shared/
│   ├── rabbitmq.yaml          # localhost RabbitMQ, default vhost
│   ├── statestore.yaml        # localhost Redis, DB 0
│   └── jaeger.yaml            # tracing config, localhost:9411
├── secrets/
│   ├── secret-store.yaml      # secretstores.local.file
│   └── secrets.json           # dev-safe Keycloak + service credentials
├── monolith-api/
│   ├── config.yaml            # Dapr config (tracing, secret scopes, API token)
│   └── binding-outbox.yaml    # cron @every 10s
├── ai-service-v2/
│   ├── config.yaml
│   └── binding-outbox.yaml
├── connector-api/
│   ├── config.yaml
│   └── oauth2.yaml            # OAuth2 middleware → local secret store
├── reverse-connector-api/
│   └── config.yaml
├── admin-api/
│   └── config.yaml
├── company-api/
│   └── config.yaml
├── job-api/
│   └── config.yaml
├── user-api/
│   ├── config.yaml
│   └── binding-auth-token.yaml
└── gateway/
    └── config.yaml
```

**No `config.redis.yaml` or `appconfig-*.yaml`** — services read config from appsettings instead.

### 3.2 Local secrets file (`secrets.json`)
```json
{
  "shared": {
    "Keycloak:Audience": "job-board-api",
    "Keycloak:SwaggerClientId": "angular-admin"
  },
  "shared-local": {
    "Keycloak:Authority": "http://localhost:{keycloak-port}/realms/job-board-local",
    "Keycloak:TokenUrl": "http://localhost:{keycloak-port}/realms/job-board-local/protocol/openid-connect/token",
    "Keycloak:ServiceClientId": "service-client",
    "Keycloak:ServiceClientSecret": "local-dev-secret"
  },
  "ai-service-v2": {
    "OpenAI:ApiKey": "sk-placeholder-set-your-key"
  }
}
```

### 3.3 Shared Dapr component examples

**rabbitmq.yaml:**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: rabbitmq.pubsub
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
    - name: connectionString
      value: "amqp://guest:guest@localhost:5672/"
    - name: enableDeadLetter
      value: "true"
    - name: exchangeKind
      value: "fanout"
```

**statestore.yaml:**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore.redis
spec:
  type: state.redis
  version: v1
  metadata:
    - name: redisHost
      value: "localhost:6379"
    - name: actorStateStore
      value: "true"
```

**secret-store.yaml:**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: vault
spec:
  type: secretstores.local.file
  version: v1
  metadata:
    - name: secretsFile
      value: "./DaprComponents/secrets/secrets.json"
    - name: nestedSeparator
      value: ":"
```
Note: component name stays `vault` so existing code referencing `"vault"` secret store works unchanged.

---

## Phase 4: Populate appsettings.json with local defaults

Add the config values that currently come from Redis (`jobboard:config:*`) as defaults in each service's appsettings.json. These are overridden at runtime by Dapr Redis config in your Proxmox workflow.

### 4.1 Global config → each service's appsettings.json

Values from `jobboard:config:global:*`:
```json
{
  "AlServiceUrl": "http://localhost:5200",
  "AdminApiUrl": "http://localhost:5262",
  "MonolithUrl": "http://localhost:5280",
  "FeatureFlags": {
    "Monolith": true
  },
  "OTEL_EXPORTER_ZIPKIN_ENDPOINT": "http://localhost:9411/api/v2/spans",
  "SeqServerUrl": "http://localhost:5341",
  "ElasticConfiguration": {
    "Uri": "http://localhost:9200"
  },
  "SMTP": {
    "Host": "localhost",
    "Port": 1025
  }
}
```

### 4.2 Service-specific config

Each service gets its own keys from `jobboard:config:{service}:*`. You'll need to provide these values — I'll set up the structure.

### 4.3 What this means for your Proxmox workflow

**Nothing changes.** The Dapr `RedisConfigurationLoader` (monolith) and `ConfigurationWatcher` (microservices) call `configuration[key] = value` directly on `IConfiguration`, which overrides appsettings values in memory at runtime. The appsettings defaults are only used when Dapr config is not active (i.e., the Aspire scenario).

---

## Phase 5: Wire Up Services with Dapr Sidecars

### 5.1 AppHost `Program.cs` — Service registration

```csharp
// --- Services with Dapr ---
var monolith = builder.AddProject<Projects.JobBoard_API>("monolith-api")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "monolith-api",
        ResourcesPaths = ImmutableHashSet.Create(
            "./DaprComponents/shared",
            "./DaprComponents/secrets",
            "./DaprComponents/monolith-api"
        ),
        Config = "./DaprComponents/monolith-api/config.yaml"
    })
    .WithReference(sqlServer)
    .WithReference(redis);

var aiService = builder.AddProject<Projects.JobBoard_AI_API>("ai-service-v2")
    .WithDaprSidecar(new DaprSidecarOptions
    {
        AppId = "ai-service-v2",
        ResourcesPaths = ImmutableHashSet.Create(
            "./DaprComponents/shared",
            "./DaprComponents/secrets",
            "./DaprComponents/ai-service-v2"
        ),
        Config = "./DaprComponents/ai-service-v2/config.yaml"
    })
    .WithReference(postgres)
    .WithReference(redis);

var adminApi = builder.AddProject<Projects.AdminApi_Service>("admin-api")
    .WithDaprSidecar(/* same pattern */)
    .WithReference(sqlServer);

var companyApi = builder.AddProject<Projects.CompanyApi_Service>("company-api")
    .WithDaprSidecar(/* same pattern */)
    .WithReference(sqlServer);

var jobApi = builder.AddProject<Projects.JobApi_Service>("job-api")
    .WithDaprSidecar(/* same pattern */)
    .WithReference(sqlServer);

var userApi = builder.AddProject<Projects.UserApi_Service>("user-api")
    .WithDaprSidecar(/* same pattern */)
    .WithReference(sqlServer);

var connectorApi = builder.AddProject<Projects.connector_api>("connector-api")
    .WithDaprSidecar(/* same pattern */);

var reverseConnectorApi = builder.AddProject<Projects.reverse_connector_api>("reverse-connector-api")
    .WithDaprSidecar(/* same pattern */);

var gateway = builder.AddProject<Projects.Gateway_Api>("gateway")
    .WithDaprSidecar(/* same pattern */)
    .WithReference(redis);

// MCP servers (no Dapr)
var monolithMcp = builder.AddProject<Projects.JobBoard_API_Mcp>("monolith-mcp");
var adminMcp = builder.AddProject<Projects.AdminApi_Mcp>("admin-api-mcp");

builder.Build().Run();
```

### 5.2 Environment handling
- Monolith's `isTesting` check: Aspire runs in Development mode, not Testing, so all infrastructure code paths execute normally
- Monolith's `AddVaultSecrets()`: Falls back gracefully when `VAULT_TOKEN` env var is missing (logs warning, skips)
- Monolith's `AddRedisConfiguration()`: Will connect to Aspire's Redis but won't find config keys → appsettings defaults remain active
- Gateway: `appsettings.Development.json` sets `UseDaprInvocation: true` → uses Dapr service invocation

---

## Phase 6: Documentation

### 6.1 Quick start
```markdown
## Quick Start (Aspire)
Prerequisites: .NET 9 SDK, .NET 10 SDK, Docker Desktop, Dapr CLI

1. `dapr init`                                        # One-time Dapr setup
2. `dotnet run --project aspire/JobBoard.AppHost`     # Launch everything
3. Open Aspire Dashboard (URL shown in console)       # Traces, logs, metrics
```

### 6.2 ADR-020: Aspire Local Development Orchestration
- Why: clone-and-run for evaluators
- How: Aspire orchestrates containers + Dapr sidecars, appsettings provides config defaults
- Coexistence: Aspire (local dev) | Dapr (runtime middleware) | Docker Compose (Proxmox deployment)

---

## What Does NOT Change
- All existing Dapr component YAMLs in `Components/` and `EnvironmentDapr/`
- All docker-compose files in `scripts/`
- All `launchSettings.json` files and Rider compound config
- All service code — zero code changes
- Proxmox infrastructure and remote connections
- Vault setup and secrets

## What Changes
- `appsettings.json` files get local default values added (overridden by Dapr at runtime in Proxmox workflow)
- `JobBoard.sln` gets new AppHost project reference
- New `aspire/` directory with AppHost project + Dapr component YAMLs

---

## Risks & Considerations

1. **Dapr CLI required** — evaluators need `dapr init` before running
2. **Keycloak realm export** — manual step, you export from your instance
3. **AI Service v2 is net10.0** — evaluators need .NET 10 SDK
4. **`Aspire.Hosting.Dapr`** — verify multi-resource-path support; may need workaround
5. **Monolith `AddRedisConfiguration()`** — will connect to Aspire Redis but find no config keys. Need to verify it handles empty Redis gracefully (likely just logs and continues since appsettings are already loaded)
6. **First-run migrations** — `MigrateDatabase<JobBoardDbContext>()` runs automatically; microservice migrations need verification

## Estimated Scope
- **New files:** ~25 (AppHost project, Dapr YAMLs, secrets file, config files)
- **Modified files:** ~10 (appsettings.json files + JobBoard.sln)
- **Service code changes:** 0
