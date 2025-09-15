using AH.Metadata.Domain.Constants;
using Dapr.Client;
using FastEndpoints;
using UserApi.Infrastructure.Auth0.Interfaces;

namespace UserApi.TimerTriggers;

public class RefreshAuthToken(
    ILogger<RefreshAuthToken> logger,
    IAuth0TokenService tokenService,
    DaprClient dapr
) : EndpointWithoutRequest<string>
{
    private const string StateStoreName = StateStores.Redis;
    private const string LockKey        = "auth0token:refresh-lock";
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(60);

    public override void Configure()
    {
        AllowAnonymous();                 // Dapr binding will call this internally
        Post("/refresh-auth-token");      // Make sure your cron component routes here
    }

    public override async Task<string> HandleAsync(CancellationToken ct)
    {
        var lockVal = Guid.NewGuid().ToString("N");

        // 1) Try to acquire a short-lived lock using ETag (optimistic concurrency)
        var entry = await dapr.GetStateEntryAsync<string>(StateStoreName, LockKey, cancellationToken: ct);
        if (!string.IsNullOrEmpty(entry.Value))
        {
            logger.LogInformation("Auth0 refresh skipped: another instance holds the lock.");
            return "skipped_locked";
        }

        var acquired = await dapr.TrySaveStateAsync(
            storeName: StateStoreName,
            key: LockKey,
            value: lockVal,
            etag: entry.ETag,            // ensure atomic write-if-unmodified
            stateOptions: null,
            metadata: new Dictionary<string, string> { ["ttlInSeconds"] = ((int)LockTtl.TotalSeconds).ToString() },
            cancellationToken: ct);

        if (!acquired)
        {
            logger.LogInformation("Auth0 refresh skipped: lock race lost");
            return "skipped_locked";
        }

        // 2) Do the refresh; always best-effort release lock
        try
        {
            logger.LogInformation("Refreshing Auth0 Management API token...");
            var token = await tokenService.RefreshAccessTokenAsync(ct);
            logger.LogInformation("Auth0 token refreshed (len={Len})", token?.Length ?? 0);
            return "ok";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auth0 token refresh failed");
            HttpContext.Response.StatusCode = 500;
            return "error";
        }
        finally
        {
            // 3) Release lock if we still own it (otherwise let TTL expire)
            try
            {
                var current = await dapr.GetStateEntryAsync<string>(StateStoreName, LockKey, cancellationToken: ct);
                if (current.Value == lockVal)
                {
                    // ETag-aware delete to avoid clobbering if someone else changed it
                    var deleted = await dapr.TryDeleteStateAsync(
                        storeName: StateStoreName,
                        key: LockKey,
                        etag: current.ETag,
                        stateOptions: null,
                        metadata: null,
                        cancellationToken: ct);

                    if (!deleted)
                        logger.LogDebug("Lock delete ETag mismatch; leaving key to expire");
                }
            }
            catch
            {
                // Ignore; TTL will clean up
            }
        }
    }
}