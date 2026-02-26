using System.Diagnostics.Metrics;
using JobBoard.Infrastructure.Diagnostics.Observability;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class MetricsServiceTests : IDisposable
{
    private readonly MetricsService _sut;
    private readonly MeterListener _listener;
    private readonly Dictionary<string, long> _counters = new();

    public MetricsServiceTests()
    {
        _sut = new MetricsService();
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "JobBoard")
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            _counters.TryGetValue(instrument.Name, out var current);
            _counters[instrument.Name] = current + measurement;
        });
        _listener.Start();
    }

    [Fact]
    public void IncrementCommandSuccess_ShouldIncrementCounter()
    {
        _sut.IncrementCommandSuccess("CreateCompanyCommand");
        _listener.RecordObservableInstruments();

        _counters.ShouldContainKey("cqrs.command.success.count");
        _counters["cqrs.command.success.count"].ShouldBe(1);
    }

    [Fact]
    public void IncrementCommandFailure_ShouldIncrementCounter()
    {
        _sut.IncrementCommandFailure("CreateCompanyCommand");
        _listener.RecordObservableInstruments();

        _counters.ShouldContainKey("cqrs.command.failure.count");
        _counters["cqrs.command.failure.count"].ShouldBe(1);
    }

    [Fact]
    public void IncrementOutboxMessagesProcessed_ShouldIncrementCounter()
    {
        _sut.IncrementOutboxMessagesProcessed();
        _listener.RecordObservableInstruments();

        _counters.ShouldContainKey("outbox.messages.processed.count");
        _counters["outbox.messages.processed.count"].ShouldBe(1);
    }

    [Fact]
    public void RecordDeadLetterMessagesFound_ShouldIncrementByCount()
    {
        _sut.RecordDeadLetterMessagesFound(5);
        _listener.RecordObservableInstruments();

        _counters.ShouldContainKey("deadletter.messages.found.count");
        _counters["deadletter.messages.found.count"].ShouldBe(5);
    }

    [Fact]
    public void IncrementCommandSuccess_CalledMultipleTimes_ShouldAccumulate()
    {
        _sut.IncrementCommandSuccess("Cmd1");
        _sut.IncrementCommandSuccess("Cmd2");
        _sut.IncrementCommandSuccess("Cmd3");
        _listener.RecordObservableInstruments();

        _counters["cqrs.command.success.count"].ShouldBe(3);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
