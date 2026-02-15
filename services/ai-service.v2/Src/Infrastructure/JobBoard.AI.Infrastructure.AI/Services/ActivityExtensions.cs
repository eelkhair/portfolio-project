using System.Diagnostics;

namespace JobBoard.AI.Infrastructure.AI.Services;

public static class ActivityExtensions
{
    public static string? GetTraceParent(this Activity activity)
        => $"00-{activity.TraceId}-{activity.SpanId}-01";
}