using JobBoard.Monolith.Contracts.Public;

namespace JobBoard.API.Infrastructure.SignalR.ResumeParse;

public interface IResumeParseNotifier
{
    Task NotifyParsedAsync(Guid resumeUId, string userId, string? currentPage, CancellationToken cancellationToken);

    Task NotifyParseFailedAsync(Guid resumeUId, string userId, string? currentPage, int attempt, int maxAttempts,
        bool isFinal, CancellationToken cancellationToken);
}
