// ReSharper disable UnusedMember.Global
namespace JobBoard.Application.Interfaces.Observability;

public interface IMetricsService
{
    void IncrementOutboxMessagesProcessed();
    void RecordDeadLetterMessagesFound(int count);
    void IncrementCommandSuccess(string commandName);
    void IncrementCommandFailure(string commandName);
}