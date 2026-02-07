using System.Diagnostics;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.Infrastructure.Diagnostics.Observability;


namespace JobBoard.Ai.Infrastructure.Diagnostics.Observability;

public class ActivitySourceFactory : IActivityFactory
{
    public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext = default)
    {
        return TracingFilters.Source.StartActivity(name, kind, parentContext);
    }
}