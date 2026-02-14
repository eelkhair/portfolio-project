using System.Text.Json;
using JobBoard.AI.Application.Interfaces.Configurations;
using StackExchange.Redis;

namespace JobBoard.AI.Infrastructure.Configuration.Services;




public sealed class RedisConfigurationStore(IConnectionMultiplexer mux) : IRedisStore
{

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, int dbId)
    {
        var db = mux.GetDatabase(dbId);
        var value = await db.StringGetAsync(key);
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
        int dbId,
        TimeSpan? ttl = null)
    {
        var _db = mux.GetDatabase(dbId);
        if (value is null) return;
        var json = JsonSerializer.Serialize(value, _jsonOptions);

        await _db.StringSetAsync(
            key,
            typeof(T) != typeof(string) ? json: value.ToString(),
            ttl ?? default(Expiration)
        );
    }

    public Task<bool> ExistsAsync(string key, int dbId)
    {
        var db = mux.GetDatabase(dbId);
        return db.KeyExistsAsync(key);
    }

    public Task RemoveAsync(string key, int dbId){
        var db = mux.GetDatabase(dbId);
        db.KeyDeleteAsync(key);
        return Task.CompletedTask;
    }
            
}