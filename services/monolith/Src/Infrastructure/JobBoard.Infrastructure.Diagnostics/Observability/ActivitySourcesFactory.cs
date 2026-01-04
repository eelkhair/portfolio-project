using System.Diagnostics;
using JobBoard.Application.Interfaces.Observability;

namespace JobBoard.Infrastructure.Diagnostics.Observability;

public class ActivitySourceFactory : IActivityFactory
{
    public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext = default)
    {
        return TracingFilters.Source.StartActivity(name, kind, parentContext);
    }
}