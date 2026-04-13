namespace JobBoard.API.Infrastructure.SignalR.ResumeParse;

public interface IResumeParseNotifier
{
    Task NotifyParsedAsync(Guid resumeUId, string userId, string? currentPage, CancellationToken cancellationToken);

    Task NotifyParseFailedAsync(Guid resumeUId, string userId, string? currentPage, int attempt, int maxAttempts,
        bool isFinal, CancellationToken cancellationToken);

    Task NotifyEmbeddedAsync(Guid resumeUId, string userId, CancellationToken cancellationToken);

    Task NotifySectionParsedAsync(Guid resumeUId, string userId, string section, string? currentPage,
        CancellationToken cancellationToken);

    Task NotifySectionFailedAsync(Guid resumeUId, string userId, string section, string? currentPage,
        CancellationToken cancellationToken);

    Task NotifyAllSectionsCompletedAsync(Guid resumeUId, string userId, string? currentPage,
        CancellationToken cancellationToken);

    Task NotifyMatchExplanationsGeneratedAsync(Guid resumeUId, string userId, CancellationToken cancellationToken);
}
