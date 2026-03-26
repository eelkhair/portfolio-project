using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Infrastructure is managed by JobBoard.AppHost.Infrastructure — start it
// first and leave it running. All containers below are persistent and should
// already be accepting connections before this AppHost launches.
// ---------------------------------------------------------------------------

var useDapr = builder.Configuration.GetValue("USE_DAPR", true);

// ---------------------------------------------------------------------------
// Connection strings
// ---------------------------------------------------------------------------

const string collectorEndpoint = "http://localhost:4327";
const string sqlServerConn = "Server=127.0.0.1,11433;Database=JobBoard;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true";
const string microSqlConn = "Server=127.0.0.1,11433;Database=local-job-board;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true";
const string postgresConn = "Host=127.0.0.1;Port=5432;Database=AiEmbeddings;Username=postgres;Password=postgres";
const string redisConn = "localhost:6379";
const string rabbitMqConn = "amqp://guest:guest@localhost:5672";
const string internalApiKey = "aspire-local-dev-key";

// Keycloak
const string keycloakAuthority = "http://localhost:9999/realms/job-board-local";
const string keycloakAudience = "jobboard-api";
const string keycloakTokenUrl = "http://localhost:9999/realms/job-board-local/protocol/openid-connect/token";
const string keycloakServiceClientId = "dapr-service-client";
const string keycloakServiceClientSecret = "Yr4ou0lgnZA1ugdxodFWfvttxcr4dupr";
const string keycloakSwaggerClientId = "angular-admin";

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

var monolith = builder.AddProject<Projects.JobBoard_API>("monolith-api")
    .WithEnvironment("ASPIRE_MODE", "true")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("ConnectionStrings__Monolith", sqlServerConn)
    .WithEnvironment("Redis__ConnectionString", redisConn)
    .WithEnvironment("RabbitMQ__Host", rabbitMqConn)
    .WithEnvironment("InternalApiKey", internalApiKey)
    .WithEnvironment("Keycloak__Authority", keycloakAuthority)
    .WithEnvironment("Keycloak__Audience", keycloakAudience)
    .WithEnvironment("Keycloak__SwaggerClientId", keycloakSwaggerClientId);

var monolithMcp = builder.AddProject<Projects.JobBoard_API_Mcp>("monolith-mcp")
    .WithEnvironment("ASPIRE_MODE", "true")
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
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
    .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
    .WithEnvironment("ConnectionStrings__Redis", redisConn)
    .WaitFor(monolith);

// ---------------------------------------------------------------------------
// Health Checks
// ---------------------------------------------------------------------------

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

var healthChecks = builder.AddProject<Projects.JobBoard_WebStatus>("health-checks")
    .WaitFor(gateway);

for (var i = 0; i < checks.Count; i++)
{
    healthChecks
        .WithEnvironment($"HealthChecksUI__HealthChecks__{i}__Name", checks[i].Name)
        .WithEnvironment($"HealthChecksUI__HealthChecks__{i}__Uri", checks[i].Uri);
}

// ---------------------------------------------------------------------------
// Frontend Apps
// ---------------------------------------------------------------------------

var jobAdmin = builder.AddNpmApp("job-admin", "../../apps/job-admin", "start")
    .WithHttpEndpoint(4200, isProxied: false)
    .WaitFor(gateway);

var jobPublic = builder.AddNpmApp("job-public", "../../apps/job-public", "start")
    .WithHttpEndpoint(3000, isProxied: false)
    .WaitFor(gateway);

// ---------------------------------------------------------------------------
// Dapr-dependent services
// ---------------------------------------------------------------------------

if (useDapr)
{
    var aiService = builder.AddProject<Projects.JobBoard_AI_API>("ai-service-v2")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
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
        .WaitFor(monolithMcp);

    var adminApi = builder.AddProject<Projects.AdminApi_Service>("admin-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("ConnectionStrings__AdminDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("admin-api"));

    var adminMcp = builder.AddProject<Projects.AdminApi_Mcp>("admin-api-mcp")
        .WithEnvironment("ASPIRE_MODE", "true")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("admin-api-mcp"))
        .WaitFor(adminApi);

    aiService
        .WithEnvironment("McpServer__MicroUrl", adminMcp.GetEndpoint("http"))
        .WaitFor(adminMcp);

    var companyApi = builder.AddProject<Projects.CompanyApi_Service>("company-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("ConnectionStrings__CompanyDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("company-api"));

    var jobApi = builder.AddProject<Projects.JobApi_Service>("job-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("ConnectionStrings__JobDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithDaprSidecar(DaprOptions("job-api"));

    var userApi = builder.AddProject<Projects.UserApi_Service>("user-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("ConnectionStrings__UserDbContext", microSqlConn)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("Keycloak__TokenUrl", keycloakTokenUrl)
        .WithEnvironment("Keycloak__ServiceClientId", keycloakServiceClientId)
        .WithEnvironment("Keycloak__ServiceClientSecret", keycloakServiceClientSecret)
        .WithDaprSidecar(DaprOptions("user-api", "./DaprComponents/user-api"));

    var connectorApi = builder.AddProject<Projects.connector_api>("connector-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("InternalApiKey", internalApiKey)
        .WithEnvironment("MonolithUrl", "http://localhost:5280")
        .WithDaprSidecar(DaprOptions("connector-api"))
        .WaitFor(monolith);

    var reverseConnectorApi = builder.AddProject<Projects.reverse_connector_api>("reverse-connector-api")
        .WithEnvironment("OTEL_COLLECTOR_ENDPOINT", collectorEndpoint)
        .WithEnvironment("Keycloak__Authority", keycloakAuthority)
        .WithEnvironment("Keycloak__Audience", keycloakAudience)
        .WithEnvironment("InternalApiKey", internalApiKey)
        .WithEnvironment("MonolithUrl", "http://localhost:5280")
        .WithDaprSidecar(DaprOptions("reverse-connector-api"))
        .WaitFor(monolith);

    gateway.WaitFor(adminApi).WaitFor(aiService);

    // Dapr Dashboard (requires dapr CLI)
    builder.AddExecutable("dapr-dashboard", "dapr", ".", "dashboard", "-p", "8888")
        .WithHttpEndpoint(8888, isProxied: false);
}

// ---------------------------------------------------------------------------

builder.Build().Run();
