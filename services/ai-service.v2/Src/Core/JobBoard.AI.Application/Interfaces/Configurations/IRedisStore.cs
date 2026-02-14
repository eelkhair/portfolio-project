namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IRedisStore
{
    Task<T?> GetAsync<T>(string key, int dbId);
    Task SetAsync<T>(string key, T value, int dbId, TimeSpan? ttl = null);
    Task<bool> ExistsAsync(string key, int dbId);
    Task RemoveAsync(string key, int dbId);
}
