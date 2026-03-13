namespace JobBoard.Infrastructure.RedisConfig;

public interface IFeatureFlagNotifier
{
    Task NotifyAsync(IReadOnlyDictionary<string, bool> flags);
}
