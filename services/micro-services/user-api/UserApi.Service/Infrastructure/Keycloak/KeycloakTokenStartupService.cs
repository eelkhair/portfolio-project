using UserApi.Infrastructure.Keycloak.Interfaces;

namespace UserApi.Infrastructure.Keycloak;

/// <summary>
/// Ensures that a Keycloak token is fetched and cached in Redis as soon as the app starts,
/// instead of waiting for the cron binding to trigger.
/// </summary>
public class KeycloakTokenStartupService(
    ILogger<KeycloakTokenStartupService> logger,
    IKeycloakTokenService tokenService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Refreshing Keycloak token on startup...");
            var token = await tokenService.RefreshAccessTokenAsync(cancellationToken);
            logger.LogInformation("Keycloak token refreshed successfully (length: {Length})", token.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh Keycloak token on startup");
            // Do not rethrow — let the app continue to run; cron binding will retry later
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
