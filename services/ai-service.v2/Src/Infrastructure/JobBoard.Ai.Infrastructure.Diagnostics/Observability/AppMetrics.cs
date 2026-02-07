using System.Diagnostics.Metrics;

namespace JobBoard.Infrastructure.Diagnostics.Observability;

public static class AppMetrics
{
    public static readonly Meter Meter = new("JobBoard");
}