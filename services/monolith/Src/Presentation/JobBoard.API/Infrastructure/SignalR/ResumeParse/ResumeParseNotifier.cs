using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace JobBoard.API.Infrastructure.SignalR.ResumeParse;

public class ResumeParseNotifier(
    IHubContext<NotificationsHub> hub,
    ActivitySource activitySource,
    ILogger<ResumeParseNotifier> log) : IResumeParseNotifier
{
    public async Task NotifyParsedAsync(Guid resumeUId, string userId, string? currentPage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var act = activitySource.StartActivity("signalr.message.send", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "ResumeParsed");
            act?.SetTag("messaging.operation", "send");

            var parent = Activity.Current;
            await hub.Clients.Group(userId).SendAsync("ResumeParsed", new
            {
                ResumeId = resumeUId,
                CurrentPage = currentPage,
                TraceParent = parent?.Id,
                TraceState = parent?.TraceStateString
            }, cancellationToken);

            act?.SetTag("enduser.id", userId);
            act?.SetTag("resume.id", resumeUId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push ResumeParsed to user {UserId} for resume {ResumeUId}", userId,
                resumeUId);
        }
    }

    public async Task NotifyParseFailedAsync(Guid resumeUId, string userId, string? currentPage, int attempt,
        int maxAttempts, bool isFinal, CancellationToken cancellationToken)
    {
        try
        {
            using var act = activitySource.StartActivity("signalr.message.send", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "ResumeParseFailed");
            act?.SetTag("messaging.operation", "send");

            var parent = Activity.Current;
            await hub.Clients.Group(userId).SendAsync("ResumeParseFailed", new
            {
                ResumeId = resumeUId,
                CurrentPage = currentPage,
                Status = isFinal ? "failed" : "retrying",
                Attempt = attempt,
                MaxAttempts = maxAttempts,
                TraceParent = parent?.Id,
                TraceState = parent?.TraceStateString
            }, cancellationToken);

            act?.SetTag("enduser.id", userId);
            act?.SetTag("resume.id", resumeUId);
            act?.SetTag("resume.parse.final_failure", isFinal);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push ResumeParseFailed to user {UserId} for resume {ResumeUId}", userId,
                resumeUId);
        }
    }

    public async Task NotifySectionParsedAsync(Guid resumeUId, string userId, string section,
        string? currentPage, CancellationToken cancellationToken)
    {
        try
        {
            using var act = activitySource.StartActivity("signalr.message.send", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "ResumeSectionParsed");
            act?.SetTag("messaging.operation", "send");

            var parent = Activity.Current;
            await hub.Clients.Group(userId).SendAsync("ResumeSectionParsed", new
            {
                ResumeId = resumeUId,
                Section = section,
                CurrentPage = currentPage,
                TraceParent = parent?.Id,
                TraceState = parent?.TraceStateString
            }, cancellationToken);

            act?.SetTag("enduser.id", userId);
            act?.SetTag("resume.id", resumeUId);
            act?.SetTag("resume.section", section);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push ResumeSectionParsed to user {UserId} for resume {ResumeUId} section {Section}",
                userId, resumeUId, section);
        }
    }

    public async Task NotifySectionFailedAsync(Guid resumeUId, string userId, string section,
        string? currentPage, CancellationToken cancellationToken)
    {
        try
        {
            using var act = activitySource.StartActivity("signalr.message.send", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "ResumeSectionFailed");
            act?.SetTag("messaging.operation", "send");

            var parent = Activity.Current;
            await hub.Clients.Group(userId).SendAsync("ResumeSectionFailed", new
            {
                ResumeId = resumeUId,
                Section = section,
                CurrentPage = currentPage,
                TraceParent = parent?.Id,
                TraceState = parent?.TraceStateString
            }, cancellationToken);

            act?.SetTag("enduser.id", userId);
            act?.SetTag("resume.id", resumeUId);
            act?.SetTag("resume.section", section);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push ResumeSectionFailed to user {UserId} for resume {ResumeUId} section {Section}",
                userId, resumeUId, section);
        }
    }

    public async Task NotifyAllSectionsCompletedAsync(Guid resumeUId, string userId, string? currentPage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var act = activitySource.StartActivity("signalr.message.send", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "ResumeAllSectionsCompleted");
            act?.SetTag("messaging.operation", "send");

            var parent = Activity.Current;
            await hub.Clients.Group(userId).SendAsync("ResumeAllSectionsCompleted", new
            {
                ResumeId = resumeUId,
                CurrentPage = currentPage,
                TraceParent = parent?.Id,
                TraceState = parent?.TraceStateString
            }, cancellationToken);

            act?.SetTag("enduser.id", userId);
            act?.SetTag("resume.id", resumeUId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push ResumeAllSectionsCompleted to user {UserId} for resume {ResumeUId}",
                userId, resumeUId);
        }
    }

    public async Task NotifyEmbeddedAsync(Guid resumeUId, string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var act = activitySource.StartActivity("signalr.message.send", ActivityKind.Producer);
            act?.SetTag("messaging.system", "signalr");
            act?.SetTag("messaging.destination.name", "ResumeEmbedded");
            act?.SetTag("messaging.operation", "send");

            var parent = Activity.Current;
            await hub.Clients.Group(userId).SendAsync("ResumeEmbedded", new
            {
                ResumeId = resumeUId,
                TraceParent = parent?.Id,
                TraceState = parent?.TraceStateString
            }, cancellationToken);

            act?.SetTag("enduser.id", userId);
            act?.SetTag("resume.id", resumeUId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to push ResumeEmbedded to user {UserId} for resume {ResumeUId}", userId,
                resumeUId);
        }
    }
}
