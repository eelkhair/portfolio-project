using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace JobBoard.Infrastructure.Diagnostics.Observability;

public class OtelLinkEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        // Export trace/span as usual
        logEvent.AddOrUpdateProperty(factory.CreateProperty(
            "traceId", activity.TraceId.ToString()));

        logEvent.AddOrUpdateProperty(factory.CreateProperty(
            "spanId", activity.SpanId.ToString()));

        if (activity.ParentSpanId != default)
        {
            logEvent.AddOrUpdateProperty(factory.CreateProperty(
                "parentSpanId", activity.ParentSpanId.ToString()));
        }
        
        if (activity.Links.Any())
        {
            var linked = activity.Links
                .Select(l => new 
                {
                    traceId = l.Context.TraceId.ToString(),
                    spanId = l.Context.SpanId.ToString()
                })
                .ToList();

            logEvent.AddOrUpdateProperty(
                factory.CreateProperty("linkedActivities", linked, destructureObjects: true)
            );
        }
    }
}