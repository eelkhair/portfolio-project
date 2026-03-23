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
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var redisCommander = builder.AddContainer("redis-commander", "rediscommander/redis-commander")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(8081, 8081, name: "ui", isProxied: false)
    .WithEnvironment("REDIS_HOSTS", "local:host.docker.internal:6379")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var rabbitMq = builder.AddContainer("rabbitmq", "rabbitmq", "4.2-management")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(5672, 5672, name: "amqp", isProxied: false)
    .WithHttpEndpoint(15672, 15672, name: "management", isProxied: false)
    .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
    .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
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

var azurite = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(10000, 10000, name: "blob", isProxied: false)
    .WithEndpoint(10001, 10001, name: "queue", isProxied: false)
    .WithEndpoint(10002, 10002, name: "table", isProxied: false)
    .WithVolume("aspire-azurite-data", "/data")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={stack}");

var grafana = builder.AddContainer("grafana", "grafana/grafana", "latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(3200, 3000, name: "ui", isProxied: false)
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithVolume("aspire-grafana-data", "/var/lib/grafana")
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

var monolith = builder.AddProject<Projects.JobBoard_API>("monolith-api")
    .WithEnvironment("ASPIRE_MODE", "true")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WaitFor(sqlServer)
    .WaitFor(redis)
    .WaitFor(rabbitMq);

var monolithMcp = builder.AddProject<Projects.JobBoard_API_Mcp>("monolith-mcp")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WaitFor(monolith);

// ---------------------------------------------------------------------------
// Gateway
// ---------------------------------------------------------------------------

var gateway = builder.AddProject<Projects.Gateway_Api>("gateway")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
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

// ---------------------------------------------------------------------------
// Dapr-dependent services
// ---------------------------------------------------------------------------

if (useDapr)
{
    var aiService = builder.AddProject<Projects.JobBoard_AI_API>("ai-service-v2")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("ai-service-v2", "./DaprComponents/ai-service-v2"))
        .WaitFor(postgres)
        .WaitFor(redis)
        .WaitFor(rabbitMq);

    var adminApi = builder.AddProject<Projects.AdminApi_Service>("admin-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("admin-api"))
        .WaitFor(rabbitMq);

    var adminMcp = builder.AddProject<Projects.AdminApi_Mcp>("admin-api-mcp")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WaitFor(adminApi);

    var companyApi = builder.AddProject<Projects.CompanyApi_Service>("company-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("company-api"))
        .WaitFor(sqlServer)
        .WaitFor(rabbitMq);

    var jobApi = builder.AddProject<Projects.JobApi_Service>("job-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("job-api"))
        .WaitFor(sqlServer)
        .WaitFor(rabbitMq);

    var userApi = builder.AddProject<Projects.UserApi_Service>("user-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("user-api", "./DaprComponents/user-api"))
        .WaitFor(sqlServer)
        .WaitFor(rabbitMq);

    var connectorApi = builder.AddProject<Projects.connector_api>("connector-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("connector-api"))
        .WaitFor(rabbitMq);

    var reverseConnectorApi = builder.AddProject<Projects.reverse_connector_api>("reverse-connector-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithDaprSidecar(DaprOptions("reverse-connector-api"))
        .WaitFor(rabbitMq);

    gateway.WaitFor(adminApi).WaitFor(aiService);

    // Dapr Dashboard (requires dapr CLI)
    builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard", "-p", "8888");
}

// ---------------------------------------------------------------------------

builder.Build().Run();
