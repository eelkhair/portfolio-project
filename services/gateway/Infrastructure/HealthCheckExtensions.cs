using JobBoard.Infrastructure.Vault;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Api.Infrastructure;

internal static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddCustomHealthChecks(this WebApplicationBuilder builder, bool isAspire = false)
    {
        var checks = builder.Services
            .AddHealthChecks()

            // -- Liveness --
            .AddCheck("self", () => HealthCheckResult.Healthy())

            // -- Redis (config store) --
            .AddRedis(
                builder.Configuration.GetConnectionString("Redis")
                ?? "192.168.1.160:6379",
                name: "Redis Config Store",
                tags: ["infrastructure"]);

        // -- Vault (secrets) — skip in Aspire (no Vault locally) --
        if (!isAspire)
            checks.AddVaultHealthCheck(tags: ["infrastructure"]);

        return builder;
    }
}