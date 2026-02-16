using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Caching.Memory;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public static class ToolHelper
{
    public static async Task<ToolResultEnvelope<T>> ExecuteCachedAsync<T>(
        IActivityFactory activityFactory,
        string operationName,
        IMemoryCache cache,
        string cacheKey,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> fetch,
        Func<T, int> countSelector,
        CancellationToken ct,
        params (string Key, object? Value)[] extraTags)
    {
        using var activity = activityFactory.StartActivity($"tool.{operationName}", ActivityKind.Internal);

        activity?.AddTag("ai.operation", operationName);

        foreach (var (key, value) in extraTags)
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

    public static async Task<T> ExecuteAsync<T>(
        IActivityFactory activityFactory,
        string operationName,
        Func<Activity?, CancellationToken, Task<T>> execute,
        CancellationToken ct,
        params (string Key, object? Value)[] extraTags)
    {
        using var activity = activityFactory.StartActivity($"tool.{operationName}", ActivityKind.Internal);

        activity?.SetTag("ai.operation", operationName);

        foreach (var (key, value) in extraTags)
            activity?.AddTag(key, value);

        return await execute(activity, ct);
    }

    public static async Task ExecuteAsync(
        IActivityFactory activityFactory,
        string operationName,
        Func<Activity?, Task> execute,
        params (string Key, object? Value)[] extraTags)
    {
        using var activity = activityFactory.StartActivity($"tool.{operationName}", ActivityKind.Internal);

        activity?.SetTag("ai.operation", operationName);

        foreach (var (key, value) in extraTags)
            activity?.AddTag(key, value);

        await execute(activity);
    }
}
