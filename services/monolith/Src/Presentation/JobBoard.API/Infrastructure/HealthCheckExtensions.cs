using JobBoard.HealthChecks;
using JobBoard.Infrastructure.Vault;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace JobBoard.API.Infrastructure;

internal static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddCustomHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();

        var rabbitUri = builder.Configuration["RabbitMQ:Host"]
                        ?? "amqp://guest:guest@192.168.1.160:5672/local";

        builder.Services
            .AddHealthChecks()

            // -- Liveness --
            .AddCheck("self", () => HealthCheckResult.Healthy())

            // -- Database --
            .AddSqlServer(
                builder.Configuration.GetConnectionString("Monolith")
                ?? throw new InvalidOperationException("DB connection missing"),
                name: "Monolith Database Check",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["database", "critical"])

            // -- Authentication --
            .AddKeycloak(o =>
            {
                o.Authority = builder.Configuration["Keycloak:Authority"]
                              ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
                o.ClientIds = ["angular-admin", "angular-public", "swagger-client"];
            })

            // -- Redis (config + state) --
            .AddRedis(
                builder.Configuration.GetConnectionString("Redis")
                ?? "192.168.1.160:6379",
                name: "Redis",
                tags: ["infrastructure"])

            // -- RabbitMQ (messaging) --
            .AddRabbitMQ(
                _ =>
                {
                    var factory = new ConnectionFactory { Uri = new Uri(rabbitUri) };
                    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                },
                name: "RabbitMQ",
                tags: ["infrastructure"])

            // -- Vault (secrets) --
            .AddVaultHealthCheck(tags: ["infrastructure"])

            // -- MCP Server --
            .AddCheck("MCP Server", () =>
            {
                var mcpUrl = builder.Configuration["McpServer:HealthUrl"] ?? "http://localhost:3333/liveness";
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var response = client.GetAsync(mcpUrl).GetAwaiter().GetResult();
                    return response.IsSuccessStatusCode
                        ? HealthCheckResult.Healthy("MCP server is responding")
                        : HealthCheckResult.Degraded($"MCP server returned {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("MCP server is unreachable", ex);
                }
            }, tags: ["infrastructure", "mcp"]);

        return builder;
    }
}
