using System.Diagnostics.Metrics;
using JobBoard.Application.Interfaces.Observability;

namespace JobBoard.Infrastructure.Diagnostics.Observability;


public class MetricsService : IMetricsService
{
    private readonly Counter<long> _outboxMessagesProcessedCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "outbox.messages.processed.count",
        unit: "{messages}",
        description: "The number of outbox messages successfully processed and published.");
    
    private readonly Counter<long> _deadLetterMessagesFoundCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "deadletter.messages.found.count",
        unit: "{messages}",
        description: "The number of new dead-letter messages found by the monitor trigger.");
    
    private readonly Counter<long> _commandSuccessCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "cqrs.command.success.count",
        unit: "{commands}",
        description: "The number of commands that have executed successfully.");
    
    private readonly Counter<long> _commandFailureCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "cqrs.command.failure.count",
        unit: "{commands}",
        description: "The number of commands that have failed during execution.");
    
    public void IncrementCommandSuccess(string commandName)
    {
        _commandSuccessCounter.Add(1, new KeyValuePair<string, object?>("command_name", commandName));
    }
    public void IncrementCommandFailure(string commandName)
    {
        _commandFailureCounter.Add(1, new KeyValuePair<string, object?>("command_name", commandName));
    }
    public void IncrementOutboxMessagesProcessed() => _outboxMessagesProcessedCounter.Add(1);
    public void RecordDeadLetterMessagesFound(int count) => _deadLetterMessagesFoundCounter.Add(count);
}