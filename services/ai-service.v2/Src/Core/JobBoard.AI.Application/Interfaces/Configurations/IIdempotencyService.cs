namespace JobBoard.AI.Application.Interfaces.Configurations;

/// <summary>
/// Abstracts idempotency check/mark pattern for Dapr pub/sub event handlers.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Returns true if the key has already been processed (processing or done).
    /// </summary>
    Task<bool> IsProcessedAsync(string prefix, string key, CancellationToken ct);

    /// <summary>
    /// Marks a key as "processing" with a short TTL (default 5 min).
    /// </summary>
    Task MarkProcessingAsync(string prefix, string key, int ttlSeconds = 300, CancellationToken ct = default);

    /// <summary>
    /// Marks a key as "done" with a long TTL (default 7 days).
    /// </summary>
    Task MarkCompletedAsync(string prefix, string key, int ttlSeconds = 604800, CancellationToken ct = default);
}
