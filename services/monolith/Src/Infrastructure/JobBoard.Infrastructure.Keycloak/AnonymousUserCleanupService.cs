using System.Diagnostics;
using JobBoard.Application.Interfaces.Infrastructure.Keycloak;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Keycloak;

/// <summary>
/// Periodically deletes throwaway Keycloak users created by the guest-signup flow
/// (tagged with attribute <c>anonymous=true</c>) once they exceed <see cref="MaxAge"/>.
/// Keeps the Keycloak user table bounded without impacting active guest sessions.
/// </summary>
public sealed class AnonymousUserCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<AnonymousUserCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);
    private const string AttributeKey = "anonymous";
    private const string AttributeValue = "true";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Anonymous user cleanup started. Interval: {Interval}, MaxAge: {MaxAge}",
            Interval, MaxAge);

        // Delay the first sweep so startup competition with migrations / outbox / Dapr
        // doesn't hammer Keycloak on every deploy.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Anonymous user cleanup iteration failed — will retry next interval");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var start = Stopwatch.GetTimestamp();
        await using var scope = scopeFactory.CreateAsyncScope();
        var keycloak = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();

        var candidates = await keycloak.FindUsersByAttributeAsync(AttributeKey, AttributeValue, ct);
        if (candidates.Count == 0)
        {
            logger.LogDebug("Anonymous cleanup: no candidates");
            return;
        }

        var cutoffMs = DateTimeOffset.UtcNow.Subtract(MaxAge).ToUnixTimeMilliseconds();
        var expired = candidates.Where(u => u.CreatedTimestamp > 0 && u.CreatedTimestamp < cutoffMs).ToList();

        if (expired.Count == 0)
        {
            logger.LogDebug("Anonymous cleanup: {Total} tagged, 0 past MaxAge", candidates.Count);
            return;
        }

        var deleted = 0;
        var failed = 0;
        foreach (var user in expired)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                await keycloak.DeleteUserAsync(user.Id, ct);
                deleted++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "Failed to delete anonymous user {UserId}", user.Id);
            }
        }

        logger.LogInformation(
            "Anonymous cleanup sweep: total={Total} expired={Expired} deleted={Deleted} failed={Failed} elapsed={Elapsed}",
            candidates.Count, expired.Count, deleted, failed, Stopwatch.GetElapsedTime(start));
    }
}
