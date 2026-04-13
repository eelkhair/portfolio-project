// ReSharper disable UnusedMember.Global
namespace JobBoard.AI.Application.Interfaces.Observability;

public interface IMetricsService
{
    void IncrementOutboxMessagesProcessed();
    void RecordDeadLetterMessagesFound(int count);
    void IncrementCommandSuccess(string commandName);
    void IncrementCommandFailure(string commandName);
    void RecordCommandDuration(string commandName, double durationMs);
    void RecordChatRequestDuration(string scope, string provider, double durationMs);
    void RecordTokenUsage(string scope, string provider, long promptTokens, long completionTokens);
    void IncrementToolCall(string scope, string toolName);
    void RecordEmbeddingDuration(double durationMs);
    void RecordResumeParseDuration(double durationMs);
    void IncrementChatRequest(string scope, string provider);
    void IncrementEmbeddingsGenerated(long count = 1);
    void IncrementResumesParsed(long count = 1);
    void IncrementResumeParseFailed();
}
