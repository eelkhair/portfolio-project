using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Elkhair.Common.Observability.Observability;

public sealed class OpenTelemetryActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity == null) 
            return;

        // Trace + Span identifiers
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentSpanId", activity.ParentSpanId.ToString()));

        // Span kind (Server/Client/Producer/Consumer/Internal)
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanKind", activity.Kind.ToString()));

        // Span Status (OK, Error)
        if (activity.Status != ActivityStatusCode.Unset)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanStatus", activity.Status.ToString()));
            if (!string.IsNullOrWhiteSpace(activity.StatusDescription))
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanStatusDescription", activity.StatusDescription));
        }

        // Tags → Elastic fields
        foreach (var tag in activity.Tags)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty($"otel.tag.{tag.Key}", tag.Value));
        }

        // Baggage → Elastic fields
        foreach (var item in activity.Baggage)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty($"otel.baggage.{item.Key}", item.Value));
        }

        // Events → Elastic fields (simplified)
        foreach (var ev in activity.Events)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                $"otel.event.{ev.Name}", 
                ev.Tags.ToDictionary(t => t.Key, t => t.Value)
            ));
        }
    }
}