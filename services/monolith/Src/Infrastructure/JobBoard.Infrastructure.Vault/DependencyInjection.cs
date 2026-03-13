using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Vault;

public static class DependencyInjection
{
    private const string DefaultVaultAddress = "http://192.168.1.115:8200";
    private const string DefaultEnginePath = "portfolio";

    public static WebApplicationBuilder AddVaultSecrets(
        this WebApplicationBuilder builder,
        string[] secretPaths)
    {
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
}
