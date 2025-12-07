using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Elkhair.Common.Observability.Observability;

public class OtelLinkEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        // Export trace/span as usual
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
            "traceId", activity.TraceId.ToString()));

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
            "spanId", activity.SpanId.ToString()));

        if (activity.ParentSpanId != default)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(
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
                propertyFactory.CreateProperty("linkedActivities", linked, destructureObjects: true)
            );
        }
    }
}