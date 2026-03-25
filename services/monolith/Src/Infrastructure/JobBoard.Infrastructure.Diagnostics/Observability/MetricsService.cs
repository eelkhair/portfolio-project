using System.Diagnostics.Metrics;
using JobBoard.Application.Interfaces.Observability;

namespace JobBoard.Infrastructure.Diagnostics.Observability;


public class MetricsService : IMetricsService
{
    // --- Outbox ---
    private readonly Counter<long> _outboxMessagesProcessedCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "outbox.messages.processed.count",
        unit: "{messages}",
        description: "The number of outbox messages successfully processed and published.");

    private readonly Counter<long> _deadLetterMessagesFoundCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "deadletter.messages.found.count",
        unit: "{messages}",
        description: "The number of new dead-letter messages found by the monitor trigger.");

    // --- CQRS ---
    private readonly Counter<long> _commandSuccessCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "cqrs.command.success.count",
        unit: "{commands}",
        description: "The number of commands that have executed successfully.");

    private readonly Counter<long> _commandFailureCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "cqrs.command.failure.count",
        unit: "{commands}",
        description: "The number of commands that have failed during execution.");

    private readonly Histogram<double> _commandDurationHistogram = AppMetrics.Meter.CreateHistogram<double>(
        name: "cqrs.command.duration",
        unit: "ms",
        description: "Duration of CQRS command/query handler execution in milliseconds.");

    private readonly Counter<long> _validationFailureCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "cqrs.validation.failure.count",
        unit: "{failures}",
        description: "The number of commands that failed FluentValidation.");

    // --- HTTP ---
    private readonly Histogram<double> _httpRequestDurationHistogram = AppMetrics.Meter.CreateHistogram<double>(
        name: "http.server.request.duration",
        unit: "ms",
        description: "Duration of inbound HTTP requests in milliseconds.");

    private readonly Counter<long> _httpRequestCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "http.server.request.count",
        unit: "{requests}",
        description: "Total inbound HTTP requests by method, route, and status code.");

    // --- CQRS ---
    public void IncrementCommandSuccess(string commandName)
    {
        _commandSuccessCounter.Add(1, new KeyValuePair<string, object?>("command_name", commandName));
    }

    public void IncrementCommandFailure(string commandName)
    {
        _commandFailureCounter.Add(1, new KeyValuePair<string, object?>("command_name", commandName));
    }

    public void RecordCommandDuration(string commandName, double durationMs)
    {
        _commandDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("command_name", commandName));
    }

    public void IncrementValidationFailure(string commandName)
    {
        _validationFailureCounter.Add(1, new KeyValuePair<string, object?>("command_name", commandName));
    }

    // --- Outbox ---
    public void IncrementOutboxMessagesProcessed() => _outboxMessagesProcessedCounter.Add(1);
    public void RecordDeadLetterMessagesFound(int count) => _deadLetterMessagesFoundCounter.Add(count);

    // --- HTTP ---
    public void RecordHttpRequestDuration(string method, string route, int statusCode, double durationMs)
    {
        _httpRequestDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("http.request.method", method),
            new KeyValuePair<string, object?>("http.route", route),
            new KeyValuePair<string, object?>("http.response.status_code", statusCode));
    }

    public void IncrementHttpRequest(string method, string route, int statusCode)
    {
        _httpRequestCounter.Add(1,
            new KeyValuePair<string, object?>("http.request.method", method),
            new KeyValuePair<string, object?>("http.route", route),
            new KeyValuePair<string, object?>("http.response.status_code", statusCode));
    }
}
