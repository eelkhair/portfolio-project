using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public static class ToolHelper
{
    /// <summary>
    /// Creates a dictionary of telemetry tags from key-value pairs.
    /// Usage: ToolHelper.Tags(("tool.company_id", companyId), ("tool.location", location))
    /// </summary>
    public static Dictionary<string, object?> Tags(params (string Key, object? Value)[] tags)
        => tags.ToDictionary(t => t.Key, t => t.Value);

    // ── Cached tools ─────────────────────────────────────────────

    public static Task<ToolResultEnvelope<T>> ExecuteCachedAsync<T>(
        IActivityFactory activityFactory,
        string operationName,
        IMemoryCache cache,
        string cacheKey,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> fetch,
        Func<T, int> countSelector,
        CancellationToken ct)
        => ExecuteCachedAsync(activityFactory, operationName, cache, cacheKey, ttl,
            fetch, countSelector, tags: [], ct);

    public static async Task<ToolResultEnvelope<T>> ExecuteCachedAsync<T>(
        IActivityFactory activityFactory,
        string operationName,
        IMemoryCache cache,
        string cacheKey,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> fetch,
        Func<T, int> countSelector,
        Dictionary<string, object?> tags,
        CancellationToken ct)
    {
        using var activity = activityFactory.StartActivity($"tool.{operationName}", ActivityKind.Internal);

        activity?.AddTag("ai.operation", operationName);

        foreach (var (key, value) in tags)
            activity?.AddTag(key, value);

        activity?.SetTag("tool.cache.key", cacheKey);
        activity?.SetTag("tool.ttl.seconds", ttl.TotalSeconds);

        if (cache.TryGetValue(cacheKey, out ToolResultEnvelope<T>? cached))
        {
            activity?.SetTag("tool.cache.hit", true);
            return cached!;
        }

        activity?.SetTag("tool.cache.hit", false);

        var result = await fetch(ct);
        var count = countSelector(result);

        var envelope = new ToolResultEnvelope<T>(result, count, DateTimeOffset.UtcNow);
        cache.Set(cacheKey, envelope, ttl);

        activity?.SetTag("tool.result.count", count);

        return envelope;
    }

    // ── Uncached tools (with return value) ───────────────────────

    public static Task<T> ExecuteAsync<T>(
        IActivityFactory activityFactory,
        string operationName,
        Func<Activity?, CancellationToken, Task<T>> execute,
        CancellationToken ct)
        => ExecuteAsync(activityFactory, operationName, execute, tags: [], ct);

    public static async Task<T> ExecuteAsync<T>(
        IActivityFactory activityFactory,
        string operationName,
        Func<Activity?, CancellationToken, Task<T>> execute,
        Dictionary<string, object?> tags,
        CancellationToken ct)
    {
        using var activity = activityFactory.StartActivity($"tool.{operationName}", ActivityKind.Internal);

        activity?.SetTag("ai.operation", operationName);

        foreach (var (key, value) in tags)
            activity?.AddTag(key, value);

        return await execute(activity, ct);
    }

    // ── Uncached tools (fire-and-forget, no return value) ────────

    public static Task ExecuteAsync(
        IActivityFactory activityFactory,
        string operationName,
        Func<Activity?, Task> execute)
        => ExecuteAsync(activityFactory, operationName, execute, tags: []);

    public static async Task ExecuteAsync(
        IActivityFactory activityFactory,
        string operationName,
        Func<Activity?, Task> execute,
        Dictionary<string, object?> tags)
    {
        using var activity = activityFactory.StartActivity($"tool.{operationName}", ActivityKind.Internal);

        activity?.SetTag("ai.operation", operationName);

        foreach (var (key, value) in tags)
            activity?.AddTag(key, value);

        await execute(activity);
    }
}
