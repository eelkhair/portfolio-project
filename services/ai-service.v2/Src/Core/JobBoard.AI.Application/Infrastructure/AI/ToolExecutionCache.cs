using JobBoard.AI.Application.Interfaces.AI;

namespace JobBoard.AI.Application.Infrastructure.AI;

public sealed class ToolExecutionCache : IToolExecutionCache
{
    private readonly Dictionary<string, object> _cache = new();

    public bool TryGet(string key, out object? value)
        => _cache.TryGetValue(key, out value);

    public void Set(string key, object value)
        => _cache[key] = value;
}