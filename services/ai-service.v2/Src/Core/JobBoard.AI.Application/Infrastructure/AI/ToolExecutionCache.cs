using JobBoard.AI.Application.Interfaces.AI;

namespace JobBoard.AI.Application.Infrastructure.AI;

public sealed record ToolCacheEntry(
    object Value,
    DateTimeOffset ExecutedAt
);
public sealed class ToolExecutionCache : IToolExecutionCache
{
    private readonly Dictionary<string, ToolCacheEntry> _cache = new();
    public bool TryGet(string key, out object? value)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            value = entry;
            return true;
        }

        value = null;
        return false;
    }

    public void Set(string key, object value)
    {
        _cache[key] = new ToolCacheEntry(
            value,
            DateTimeOffset.UtcNow
        );
    }
}