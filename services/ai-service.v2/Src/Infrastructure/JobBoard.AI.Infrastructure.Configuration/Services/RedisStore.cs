using System.Text.Json;
using StackExchange.Redis;

namespace JobBoard.AI.Infrastructure.Configuration.Services;


public interface IRedisJsonStore
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task<bool> ExistsAsync(string key);
    Task RemoveAsync(string key);
}


public sealed class RedisJsonStore(IConnectionMultiplexer mux) : IRedisJsonStore
{
    private readonly IDatabase _db = mux.GetDatabase(1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;

        // Handle string type specially (matches SetAsync behavior)
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value.ToString();
        }

        return JsonSerializer.Deserialize<T>(
            value.ToString(),
            _jsonOptions
        );
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null)
    {
        if (value is null) return;
        var json = JsonSerializer.Serialize(value, _jsonOptions);

        await _db.StringSetAsync(
            key,
            typeof(T) != typeof(string) ? json: value.ToString(),
            ttl ?? default(Expiration)
        );
    }

    public Task<bool> ExistsAsync(string key)
        => _db.KeyExistsAsync(key);

    public Task RemoveAsync(string key)
        => _db.KeyDeleteAsync(key);
}