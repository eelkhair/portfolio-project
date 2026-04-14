using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Infrastructure (persistent containers)
// ---------------------------------------------------------------------------

const string stack = "jobboard-aspire";

var sqlServer = builder.AddContainer("sqlserver", "mcr.microsoft.com/mssql/server", "2022-latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(11433, 1433, name: "tcp", isProxied: false)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("MSSQL_SA_PASSWORD", "YourStrong!Passw0rd")
    .WithVolume("aspire-sqlserver-data", "/var/opt/mssql")
    .WithBindMount("./SqlServerInit/backups", "/seed-backups", isReadOnly: true)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var postgres = builder.AddContainer("postgres", "ankane/pgvector", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(5432, 5432, name: "tcp", isProxied: false)
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithEnvironment("POSTGRES_DB", "AiEmbeddings")
    .WithVolume("postgres-data", "/var/lib/postgresql/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var redis = builder.AddContainer("redis", "redis", "8.2")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(6379, 6379, name: "tcp", isProxied: false)
    .WithContainerRuntimeArgs("--health-cmd", "redis-cli ping || exit 1")
    .WithContainerRuntimeArgs("--health-interval", "2s")
    .WithContainerRuntimeArgs("--health-retries", "5")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var seedRunner = builder.AddContainer("seed-runner", "ghcr.io/eelkhair/seed-runner", "1.0")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount("./SeedRunner/entrypoint.sh", "/entrypoint.sh", isReadOnly: true)
    .WithBindMount("./RedisInit/seed.sh", "/seeds/seed-redis.sh", isReadOnly: true)
    .WithBindMount("./SqlServerInit/seed-sqlserver.sh", "/seeds/seed-sqlserver.sh", isReadOnly: true)
    .WithBindMount("./PostgresInit/seed-postgres.sh", "/seeds/seed-postgres.sh", isReadOnly: true)
    .WithBindMount("./PostgresInit/backups", "/seed-backups", isReadOnly: true)
    .WithEntrypoint("/bin/bash")
    .WithArgs("/entrypoint.sh")
    .WithHttpEndpoint(18080, 8080, name: "health", isProxied: false)
    .WaitFor(redis)
    .WaitFor(sqlServer)
    .WaitFor(postgres)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var redisCommander = builder.AddContainer("redis-commander", "rediscommander/redis-commander")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(8081, 8081, name: "ui", isProxied: false)
    .WithEnvironment("REDIS_HOSTS", "main:host.docker.internal:6379:0,config:host.docker.internal:6379:1,ai:host.docker.internal:6379:2")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var rabbitMq = builder.AddContainer("rabbitmq", "rabbitmq", "4.2-management")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(5672, 5672, name: "amqp", isProxied: false)
    .WithHttpEndpoint(15672, 15672, name: "management", isProxied: false)
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
    .WithContainerRuntimeArgs("--health-cmd", "rabbitmq-diagnostics -q check_running && rabbitmq-diagnostics -q check_local_alarms")
    .WithContainerRuntimeArgs("--health-interval", "3s")
    .WithContainerRuntimeArgs("--health-timeout", "5s")
    .WithContainerRuntimeArgs("--health-retries", "15")
    .WithContainerRuntimeArgs("--health-start-period", "15s")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var keycloakPassword = builder.AddParameter("keycloak-password", "admin");
var keycloak = builder.AddKeycloak("keycloak", 9999, adminPassword: keycloakPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRealmImport("./KeycloakRealm")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.64.0")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(16686, 16686, name: "ui", isProxied: false)
    .WithEnvironment("LOG_LEVEL", "debug")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(4327, 4317, name: "otlp-grpc", isProxied: false)
    .WithEndpoint(4328, 4318, name: "otlp-http", isProxied: false)
    .WithEndpoint(8889, 8889, name: "prometheus", isProxied: false)
    .WithBindMount("./OtelCollector/otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml", isReadOnly: true)
    .WaitFor(jaeger)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(8025, 8025, name: "ui", isProxied: false)
    .WithEndpoint(1025, 1025, name: "smtp", isProxied: false)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var elasticsearch = builder.AddContainer("elasticsearch", "docker.elastic.co/elasticsearch/elasticsearch", "8.17.0")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(9200, 9200, name: "http", isProxied: false)
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithVolume("aspire-elasticsearch-data", "/usr/share/elasticsearch/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var seq = builder.AddContainer("seq", "datalust/seq", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(5341, 80, name: "ui", isProxied: false)
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithEnvironment("SEQ_FIRSTRUN_NOAUTHENTICATION", "true")
    .WithVolume("aspire-seq-data", "/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(10000, 10000, name: "blob", isProxied: false)
    .WithEndpoint(10001, 10001, name: "queue", isProxied: false)
    .WithEndpoint(10002, 10002, name: "table", isProxied: false)
    .WithVolume("aspire-azurite-data", "/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(9090, 9090, name: "ui", isProxied: false)
    .WithBindMount("./PrometheusConfig/prometheus.yml", "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithVolume("aspire-prometheus-data", "/prometheus")
    .WaitFor(otelCollector)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(3200, 3000, name: "ui", isProxied: false)
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithVolume("aspire-grafana-data", "/var/lib/grafana")
    .WithBindMount("./GrafanaProvisioning/datasources", "/etc/grafana/provisioning/datasources", isReadOnly: true)
    .WithBindMount("./GrafanaProvisioning/dashboards", "/etc/grafana/provisioning/dashboards", isReadOnly: true)
    .WithBindMount("./GrafanaDashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
    .WaitFor(prometheus)
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var useDapr = builder.Configuration.GetValue("USE_DAPR", true);

// Health check entries — core services always, Dapr-dependent services only when USE_DAPR=true
var checks = new List<(string Name, string Uri)>
{
    ("Gateway",              "http://localhost:5238/healthzEndpoint"),
    ("Monolith API",         "http://localhost:5280/healthzEndpoint"),
    ("Monolith MCP",         "http://localhost:3333/healthzEndpoint"),
};

if (useDapr)
{
    checks.AddRange([
        ("AI Service V2",        "http://localhost:5200/healthzEndpoint"),
        ("Admin API",            "http://localhost:5262/healthzEndpoint"),
        ("Company API",          "http://localhost:5272/healthzEndpoint"),
        ("Job API",              "http://localhost:5282/healthzEndpoint"),
        ("User API",             "http://localhost:5292/healthzEndpoint"),
        ("Connector API",        "http://localhost:5284/healthzEndpoint"),
        ("Reverse Connector API","http://localhost:5190/healthzEndpoint"),
    ]);
}

var healthChecks = builder.AddProject<Projects.JobBoard_WebStatus>("health-checks");

for (var i = 0; i < checks.Count; i++)
{
    healthChecks
        .WithEnvironment($"HealthChecksUI__HealthChecks__{i}__Name", checks[i].Name)
        .WithEnvironment($"HealthChecksUI__HealthChecks__{i}__Uri", checks[i].Uri);
}

// ---------------------------------------------------------------------------
// Dapr
// ---------------------------------------------------------------------------

var sharedComponents = "./DaprComponents/shared";
var secretsComponents = "./DaprComponents/secrets";

DaprSidecarOptions DaprOptions(string appId, params string[] extraPaths)
{
    var paths = new List<string> { sharedComponents, secretsComponents };
    paths.AddRange(extraPaths);

    return new DaprSidecarOptions
    {
        AppId = appId,
        ResourcesPaths = paths.ToImmutableHashSet(),
        Config = "./DaprComponents/shared/config.yaml"
    };
}

// ---------------------------------------------------------------------------
// Monolith
// ---------------------------------------------------------------------------

const string collectorEndpoint = "http://localhost:4327";

// Connection strings — passed as env vars so they're available before Dapr sidecar starts
const string sqlServerConn = "Server=127.0.0.1,11433;Database=job-board-monolith;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true";
const string microSqlConn = "Server=127.0.0.1,11433;Database=job-board;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true";
const string postgresConn = "Host=127.0.0.1;Port=5432;Database=AiEmbeddings;Username=postgres;Password=postgres";
const string redisConn = "localhost:6379";
const string rabbitMqConn = "amqp://guest:guest@localhost:5672";
const string internalApiKey = "aspire-local-dev-key";
const string seqUrl = "http://localhost:5341";

// Keycloak — must be available at startup before Dapr vault loads
const string keycloakAuthority = "http://localhost:9999/realms/job-board-local";
const string keycloakAudience = "jobboard-api";
const string keycloakTokenUrl = "http://localhost:9999/realms/job-board-local/protocol/openid-connect/token";
const string keycloakServiceClientId = "dapr-service-client";
const string keycloakServiceClientSecret = "Yr4ou0lgnZA1ugdxodFWfvttxcr4dupr";
const string keycloakSwaggerClientId = "angular-admin";

var monolith = builder.AddProject<Projects.JobBoard_API>("monolith-api")
    .WithEnvironment("ASPIRE_MODE", "true")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
    .WithEnvironment("ConnectionStrings__Monolith", sqlServerConn)
    .WithEnvironment("Redis__ConnectionString", redisConn)
    .WithEnvironment("RabbitMQ__Host", rabbitMqConn)
    .WithEnvironment("InternalApiKey", internalApiKey)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", keycloakAudience)
    .WithEnvironment("Keycloak__SwaggerClientId", keycloakSwaggerClientId)
    .WaitFor(seedRunner)
    .WaitFor(rabbitMq)
    .WaitFor(keycloak);

var monolithMcp = builder.AddProject<Projects.JobBoard_API_Mcp>("monolith-mcp")
    .WithEnvironment("ASPIRE_MODE", "true")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
    .WithEnvironment("ConnectionStrings__Monolith", sqlServerConn)
    .WithEnvironment("Redis__ConnectionString", redisConn)
    .WithEnvironment("RabbitMQ__Host", rabbitMqConn)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", keycloakAudience)
    .WaitFor(monolith);

// ---------------------------------------------------------------------------
// Gateway
// ---------------------------------------------------------------------------

var gateway = builder.AddProject<Projects.Gateway_Api>("gateway")
    .WithEnvironment("ASPIRE_MODE", "true")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
    .WithEnvironment("ConnectionStrings__Redis", redisConn)
    .WithEnvironment("AdminApiUrl", "http://localhost:5262")
    .WaitFor(seedRunner)
    .WaitFor(monolith);

// ---------------------------------------------------------------------------
// Frontend Apps
// ---------------------------------------------------------------------------

var jobAdmin = builder.AddNpmApp("job-admin", "../../apps/job-admin", "start")
    .WithHttpEndpoint(4200, isProxied: false)
    .WaitFor(gateway)
    .WaitFor(keycloak);

var jobPublic = builder.AddNpmApp("job-public", "../../apps/job-public", "start")
    .WithHttpEndpoint(3000, isProxied: false)
    .WaitFor(gateway)
    .WaitFor(keycloak);

var landingNext = builder.AddNpmApp("landing-next", "../../apps/landing-next", "dev")
    .WithHttpEndpoint(3001, isProxied: false)
    .WaitFor(gateway);

// ---------------------------------------------------------------------------
// Dapr-dependent services
// ---------------------------------------------------------------------------

if (useDapr)
{
    var aiService = builder.AddProject<Projects.JobBoard_AI_API>("ai-service-v2")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("McpServer__IntegrationUrl", monolithMcp.GetEndpoint("http"))
        .WithEnvironment("ConnectionStrings__ai-db", postgresConn)
        .WithEnvironment("ConnectionStrings__Monolith", sqlServerConn)
        .WithEnvironment("Redis__ConnectionString", redisConn)
        .WithEnvironment("InternalApiKey", internalApiKey)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("AIProvider", "claude")
        .WithEnvironment("AIModel", "claude-sonnet-4-20250514")
        .WithEnvironment("AI__CLAUDE_API_KEY", builder.Configuration["AI:CLAUDE_API_KEY"] ?? "")
        .WithEnvironment("OpenAI__ApiKey", builder.Configuration["OpenAI:ApiKey"] ?? "")
        .WithDaprSidecar(DaprOptions("ai-service-v2", "./DaprComponents/ai-service-v2"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak)
        .WaitFor(monolithMcp);

    var adminApi = builder.AddProject<Projects.AdminApi_Service>("admin-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("ConnectionStrings__AdminDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("admin-api"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak);

    var adminMcp = builder.AddProject<Projects.AdminApi_Mcp>("admin-api-mcp")
        .WithEnvironment("ASPIRE_MODE", "true")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("admin-api-mcp"))
        .WaitFor(adminApi);

    aiService
        .WithEnvironment("McpServer__MicroUrl", adminMcp.GetEndpoint("http"))
        .WaitFor(adminMcp);

    var companyApi = builder.AddProject<Projects.CompanyApi_Service>("company-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("ConnectionStrings__CompanyDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("company-api"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak);

    var jobApi = builder.AddProject<Projects.JobApi_Service>("job-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("ConnectionStrings__JobDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("job-api"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak);

    var userApi = builder.AddProject<Projects.UserApi_Service>("user-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("ConnectionStrings__UserDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("Keycloak__TokenUrl", keycloakTokenUrl)
        .WithEnvironment("Keycloak__ServiceClientId", keycloakServiceClientId)
        .WithEnvironment("Keycloak__ServiceClientSecret", keycloakServiceClientSecret)
        .WithDaprSidecar(DaprOptions("user-api", "./DaprComponents/user-api"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak);

    var connectorApi = builder.AddProject<Projects.connector_api>("connector-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("InternalApiKey", internalApiKey)
        .WithEnvironment("MonolithUrl", "http://localhost:5280")
        .WithDaprSidecar(DaprOptions("connector-api"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak);

    var reverseConnectorApi = builder.AddProject<Projects.reverse_connector_api>("reverse-connector-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("SEQ_URL", seqUrl)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("InternalApiKey", internalApiKey)
        .WithEnvironment("MonolithUrl", "http://localhost:5280")
        .WithDaprSidecar(DaprOptions("reverse-connector-api"))
        .WaitFor(seedRunner)
        .WaitFor(rabbitMq)
        .WaitFor(keycloak);

    gateway.WaitFor(adminApi).WaitFor(aiService);

    // Dapr Dashboard (requires dapr CLI)
    builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard", "-p", "8888")
        .WithHttpEndpoint(8888, isProxied: false);
}

// ---------------------------------------------------------------------------

builder.Build().Run();
