using Microsoft.Extensions.Diagnostics.HealthChecks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace JobBoard.Infrastructure.Vault;

public class VaultHealthCheck(string vaultAddress, string vaultToken) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var authMethod = new TokenAuthMethodInfo(vaultToken);
            var settings = new VaultClientSettings(vaultAddress, authMethod);
            var client = new VaultClient(settings);

            var health = await client.V1.System.GetHealthStatusAsync();

            return health.Initialized && !health.Sealed
                ? HealthCheckResult.Healthy($"Vault is unsealed at {vaultAddress}")
                : HealthCheckResult.Unhealthy($"Vault sealed={health.Sealed}, initialized={health.Initialized}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Vault unreachable at {vaultAddress}", ex);
        }
    }
}
