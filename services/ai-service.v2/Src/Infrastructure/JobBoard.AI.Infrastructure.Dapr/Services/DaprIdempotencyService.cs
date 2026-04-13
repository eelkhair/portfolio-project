using AH.Metadata.Domain.Constants;
using Dapr.Client;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Infrastructure.Dapr.Services;

public class DaprIdempotencyService(DaprClient daprClient) : IIdempotencyService
{
    public async Task<bool> IsProcessedAsync(string prefix, string key, CancellationToken ct)
    {
        var stateKey = $"{prefix}{key}";
        var existing = await daprClient.GetStateAsync<string>(StateStores.Redis, stateKey, cancellationToken: ct);
        return existing is not null;
    }

    public async Task MarkProcessingAsync(string prefix, string key, int ttlSeconds = 300, CancellationToken ct = default)
    {
        var stateKey = $"{prefix}{key}";
        await daprClient.SaveStateAsync(
            StateStores.Redis,
            stateKey,
            "processing",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = ttlSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            cancellationToken: ct);
    }

    public async Task MarkCompletedAsync(string prefix, string key, int ttlSeconds = 604800, CancellationToken ct = default)
    {
        var stateKey = $"{prefix}{key}";
        await daprClient.SaveStateAsync(
            StateStores.Redis,
            stateKey,
            "done",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["ttlInSeconds"] = ttlSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            cancellationToken: ct);
    }
}
