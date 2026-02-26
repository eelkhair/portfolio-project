using System.Diagnostics;
using JobBoard.Infrastructure.Diagnostics.Observability;
using Serilog.Core;
using Serilog.Events;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class OpenTelemetryActivityEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("TestSource");
    private readonly ActivityListener _listener;
    private readonly OpenTelemetryActivityEnricher _sut = new();

    public OpenTelemetryActivityEnricherTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddTraceId()
    {
        using var activity = _source.StartActivity("TestOp");
        activity.ShouldNotBeNull();

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldContainKey("TraceId");
        GetStringValue(logEvent, "TraceId").ShouldBe(activity.TraceId.ToString());
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddSpanId()
    {
        using var activity = _source.StartActivity("TestOp");
        activity.ShouldNotBeNull();

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldContainKey("SpanId");
        GetStringValue(logEvent, "SpanId").ShouldBe(activity.SpanId.ToString());
    }

    [Fact]
    public void Enrich_WithActivityTags_ShouldAddTagProperties()
    {
        using var activity = _source.StartActivity("TestOp");
        activity.ShouldNotBeNull();
        activity.SetTag("user.id", "user-123");

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldContainKey("otel.tag.user.id");
    }

    [Fact]
    public void Enrich_WithNoActivity_ShouldNotAddProperties()
    {
        // Ensure no current activity
        Activity.Current = null;

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldNotContainKey("TraceId");
        logEvent.Properties.ShouldNotContainKey("SpanId");
    }

    [Fact]
    public void Enrich_WithSpanKind_ShouldAddSpanKindProperty()
    {
        using var activity = _source.StartActivity("TestOp", ActivityKind.Server);
        activity.ShouldNotBeNull();

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldContainKey("SpanKind");
        GetStringValue(logEvent, "SpanKind").ShouldBe("Server");
    }

    private static (LogEvent, ILogEventPropertyFactory) CreateLogEvent()
    {
        var factory = new LogEventPropertyFactory();
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            []);
        return (logEvent, factory);
    }

    private static string GetStringValue(LogEvent logEvent, string key)
    {
        return ((ScalarValue)logEvent.Properties[key]).Value?.ToString() ?? "";
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }
}

[Trait("Category", "Unit")]
public class OtelLinkEnricherTests : IDisposable
{
    private readonly ActivitySource _source = new("TestLinkSource");
    private readonly ActivityListener _listener;
    private readonly OtelLinkEnricher _sut = new();

    public OtelLinkEnricherTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddTraceId()
    {
        using var activity = _source.StartActivity("TestOp");
        activity.ShouldNotBeNull();

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldContainKey("traceId");
        GetStringValue(logEvent, "traceId").ShouldBe(activity.TraceId.ToString());
    }

    [Fact]
    public void Enrich_WithActiveActivity_ShouldAddSpanId()
    {
        using var activity = _source.StartActivity("TestOp");
        activity.ShouldNotBeNull();

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldContainKey("spanId");
        GetStringValue(logEvent, "spanId").ShouldBe(activity.SpanId.ToString());
    }

    [Fact]
    public void Enrich_WithNoActivity_ShouldNotAddProperties()
    {
        Activity.Current = null;

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        logEvent.Properties.ShouldNotContainKey("traceId");
    }

    [Fact]
    public void Enrich_WithParentSpanId_ShouldAddParentSpanId()
    {
        using var parent = _source.StartActivity("Parent");
        using var child = _source.StartActivity("Child");
        child.ShouldNotBeNull();

        var (logEvent, factory) = CreateLogEvent();

        _sut.Enrich(logEvent, factory);

        if (child.ParentSpanId != default)
        {
            logEvent.Properties.ShouldContainKey("parentSpanId");
        }
    }

    private static (LogEvent, ILogEventPropertyFactory) CreateLogEvent()
    {
        var factory = new LogEventPropertyFactory();
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            []);
        return (logEvent, factory);
    }

    private static string GetStringValue(LogEvent logEvent, string key)
    {
        return ((ScalarValue)logEvent.Properties[key]).Value?.ToString() ?? "";
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }
}

/// <summary>
/// Simple ILogEventPropertyFactory for testing Serilog enrichers.
/// </summary>
internal class LogEventPropertyFactory : ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        return new LogEventProperty(name, new ScalarValue(value));
    }
}
