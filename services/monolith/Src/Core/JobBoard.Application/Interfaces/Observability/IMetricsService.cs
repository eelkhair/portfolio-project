// ReSharper disable UnusedMember.Global
namespace JobBoard.Application.Interfaces.Observability;

public interface IMetricsService
{
    void IncrementOutboxMessagesProcessed();
    void RecordDeadLetterMessagesFound(int count);
    void IncrementCommandSuccess(string commandName);
    void IncrementCommandFailure(string commandName);
    void RecordCommandDuration(string commandName, double durationMs);
    void RecordHttpRequestDuration(string method, string route, int statusCode, double durationMs);
    void IncrementHttpRequest(string method, string route, int statusCode);
    void IncrementValidationFailure(string commandName);
}