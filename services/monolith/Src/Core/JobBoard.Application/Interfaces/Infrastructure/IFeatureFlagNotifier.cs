namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IFeatureFlagNotifier
{
    Task NotifyAsync(IReadOnlyDictionary<string, bool> flags);
}
