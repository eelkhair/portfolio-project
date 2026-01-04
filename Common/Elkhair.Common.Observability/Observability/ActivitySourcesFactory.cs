using System.Diagnostics;

namespace Elkhair.Common.Observability.Observability;

public interface IActivityFactory
{
    Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext = default);
}
public class ActivitySourceFactory : IActivityFactory
{
    public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext = default)
    {
        return TracingFilters.Source.StartActivity(name, kind, parentContext);
    }
}