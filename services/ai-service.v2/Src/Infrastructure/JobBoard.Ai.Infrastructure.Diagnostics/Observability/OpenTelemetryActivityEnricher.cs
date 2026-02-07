using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

public sealed class OpenTelemetryActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var activity = Activity.Current;
        if (activity == null) 
            return;

        // Trace + Span identifiers
        logEvent.AddPropertyIfAbsent(factory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(factory.CreateProperty("SpanId", activity.SpanId.ToString()));
        logEvent.AddPropertyIfAbsent(factory.CreateProperty("ParentSpanId", activity.ParentSpanId.ToString()));

        // Span kind (Server/Client/Producer/Consumer/Internal)
        logEvent.AddPropertyIfAbsent(factory.CreateProperty("SpanKind", activity.Kind.ToString()));

        // Span Status (OK, Error)
        if (activity.Status != ActivityStatusCode.Unset)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty("SpanStatus", activity.Status.ToString()));
            if (!string.IsNullOrWhiteSpace(activity.StatusDescription))
                logEvent.AddPropertyIfAbsent(factory.CreateProperty("SpanStatusDescription", activity.StatusDescription));
        }

        // Tags → Elastic fields
        foreach (var tag in activity.Tags)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty($"otel.tag.{tag.Key}", tag.Value));
        }

        // Baggage → Elastic fields
        foreach (var item in activity.Baggage)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty($"otel.baggage.{item.Key}", item.Value));
        }

        // Events → Elastic fields (simplified)
        foreach (var ev in activity.Events)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty(
                $"otel.event.{ev.Name}", 
                ev.Tags.ToDictionary(t => t.Key, t => t.Value)
            ));
        }
    }
}