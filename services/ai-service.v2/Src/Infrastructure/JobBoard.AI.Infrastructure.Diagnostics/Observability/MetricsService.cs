using System.Diagnostics.Metrics;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.Infrastructure.Diagnostics.Observability;

namespace JobBoard.AI.Infrastructure.Diagnostics.Observability;


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

    // --- AI Chat ---
    private readonly Histogram<double> _chatDurationHistogram = AppMetrics.Meter.CreateHistogram<double>(
        name: "ai.chat.duration",
        unit: "ms",
        description: "Duration of AI chat requests in milliseconds.");

    private readonly Counter<long> _chatRequestCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.chat.request.count",
        unit: "{requests}",
        description: "Total AI chat requests by scope and provider.");

    private readonly Counter<long> _promptTokenCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.chat.tokens.prompt",
        unit: "{tokens}",
        description: "Total prompt tokens consumed by AI chat requests.");

    private readonly Counter<long> _completionTokenCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.chat.tokens.completion",
        unit: "{tokens}",
        description: "Total completion tokens consumed by AI chat requests.");

    private readonly Counter<long> _toolCallCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.tool.call.count",
        unit: "{calls}",
        description: "Total AI tool/function calls by scope and tool name.");

    // --- Resume Pipeline ---
    private readonly Histogram<double> _embeddingDurationHistogram = AppMetrics.Meter.CreateHistogram<double>(
        name: "ai.embedding.duration",
        unit: "ms",
        description: "Duration of resume embedding generation in milliseconds.");

    private readonly Histogram<double> _resumeParseDurationHistogram = AppMetrics.Meter.CreateHistogram<double>(
        name: "ai.resume.parse.duration",
        unit: "ms",
        description: "Duration of full resume parse pipeline in milliseconds.");

    private readonly Counter<long> _embeddingsGeneratedCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.embeddings.generated.count",
        unit: "{embeddings}",
        description: "Total number of embeddings generated (jobs + resumes).");

    private readonly Counter<long> _resumesParsedCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.resumes.parsed.count",
        unit: "{resumes}",
        description: "Total number of resumes parsed.");

    private readonly Counter<long> _resumeParseFailedCounter = AppMetrics.Meter.CreateCounter<long>(
        name: "ai.resumes.parse.failed.count",
        unit: "{resumes}",
        description: "Total number of resume parse failures.");

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

    // --- Outbox ---
    public void IncrementOutboxMessagesProcessed() => _outboxMessagesProcessedCounter.Add(1);
    public void RecordDeadLetterMessagesFound(int count) => _deadLetterMessagesFoundCounter.Add(count);

    // --- AI Chat ---
    public void RecordChatRequestDuration(string scope, string provider, double durationMs)
    {
        _chatDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("ai.chat.scope", scope),
            new KeyValuePair<string, object?>("ai.provider", provider));
    }

    public void IncrementChatRequest(string scope, string provider)
    {
        _chatRequestCounter.Add(1,
            new KeyValuePair<string, object?>("ai.chat.scope", scope),
            new KeyValuePair<string, object?>("ai.provider", provider));
    }

    public void RecordTokenUsage(string scope, string provider, long promptTokens, long completionTokens)
    {
        _promptTokenCounter.Add(promptTokens,
            new KeyValuePair<string, object?>("ai.chat.scope", scope),
            new KeyValuePair<string, object?>("ai.provider", provider));
        _completionTokenCounter.Add(completionTokens,
            new KeyValuePair<string, object?>("ai.chat.scope", scope),
            new KeyValuePair<string, object?>("ai.provider", provider));
    }

    public void IncrementToolCall(string scope, string toolName)
    {
        _toolCallCounter.Add(1,
            new KeyValuePair<string, object?>("ai.chat.scope", scope),
            new KeyValuePair<string, object?>("ai.tool.name", toolName));
    }

    // --- Resume Pipeline ---
    public void RecordEmbeddingDuration(double durationMs)
    {
        _embeddingDurationHistogram.Record(durationMs);
    }

    public void RecordResumeParseDuration(double durationMs)
    {
        _resumeParseDurationHistogram.Record(durationMs);
    }

    public void IncrementEmbeddingsGenerated(long count = 1)
    {
        _embeddingsGeneratedCounter.Add(count);
    }

    public void IncrementResumesParsed(long count = 1)
    {
        _resumesParsedCounter.Add(count);
    }

    public void IncrementResumeParseFailed()
    {
        _resumeParseFailedCounter.Add(1);
    }
}
