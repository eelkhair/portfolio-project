using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Vault;

public static class DependencyInjection
{
    private const string DefaultVaultAddress = "http://192.168.1.115:8200";
    private const string DefaultEnginePath = "portfolio";

    public static WebApplicationBuilder AddVaultSecrets(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        var envSuffix = GetEnvironmentSuffix(builder.Environment.EnvironmentName);
        var secretPaths = new[] { $"{serviceName}-{envSuffix}", serviceName, $"shared-{envSuffix}", "shared" };

        var vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? DefaultVaultAddress;
        var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");

        if (string.IsNullOrEmpty(vaultToken))
        {
            var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("VaultSecrets");
            logger.LogWarning("VAULT_TOKEN not set — skipping Vault, using user-secrets fallback");
            return builder;
        }

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var log = loggerFactory.CreateLogger("VaultSecrets");

        ((IConfigurationBuilder)builder.Configuration).Add(new VaultConfigurationSource(
            vaultAddress,
            vaultToken,
            DefaultEnginePath,
            secretPaths,
            log));

        return builder;
    }

    public static IHealthChecksBuilder AddVaultHealthCheck(
        this IHealthChecksBuilder builder,
        string? name = "Vault",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        var vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? DefaultVaultAddress;
        var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? "";

        return builder.AddCheck(
            name ?? "Vault",
            new VaultHealthCheck(vaultAddress, vaultToken),
            failureStatus,
            tags ?? ["infrastructure"]);
    }

    private static string GetEnvironmentSuffix(string environmentName) =>
        environmentName switch
        {
            "Development" => "local",
            "Production" => "prod",
            _ => environmentName.ToLowerInvariant()
        };
}
