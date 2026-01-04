using Microsoft.Extensions.Hosting;
using UserApi.Infrastructure.Auth0.Interfaces;

namespace UserApi.Infrastructure.Auth0;

/// <summary>
/// Ensures that an Auth0 token is fetched and cached in Redis as soon as the app starts,
/// instead of waiting for the cron binding to trigger.
/// </summary>
public class Auth0TokenStartupService(
    ILogger<Auth0TokenStartupService> logger,
    IAuth0TokenService tokenService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Refreshing Auth0 token on startup...");
            var token = await tokenService.RefreshAccessTokenAsync(cancellationToken);
            logger.LogInformation("Auth0 token refreshed successfully (length: {Length})", token.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh Auth0 token on startup");
            // Do not rethrow — let the app continue to run; cron binding will retry later
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}