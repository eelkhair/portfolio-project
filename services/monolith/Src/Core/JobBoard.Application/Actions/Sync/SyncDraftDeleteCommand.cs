using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Sync;

public class SyncDraftDeleteCommand : BaseCommand<Unit>
{
    public Guid DraftId { get; set; }
    public Guid CompanyId { get; set; }
}

/// <summary>
/// Reverse-sync handler: deletes a draft from a microservice event.
/// Does NOT call OutboxPublisher to prevent infinite sync loops.
/// Idempotent: returns gracefully if draft not found.
/// </summary>
public class SyncDraftDeleteCommandHandler(IHandlerContext handlerContext)
    : BaseCommandHandler(handlerContext), IHandler<SyncDraftDeleteCommand, Unit>
{
    public async Task<Unit> HandleAsync(SyncDraftDeleteCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("sync.draft.id", command.DraftId);
        Activity.Current?.SetTag("sync.draft.companyId", command.CompanyId);
        Logger.LogInformation("Reverse-sync: deleting draft {DraftId} for company {CompanyId}",
            command.DraftId, command.CompanyId);

        var dbSet = ((IJobBoardQueryDbContext)Context).Drafts;

        var draft = await dbSet
            .FirstOrDefaultAsync(d => d.Id == command.DraftId && d.CompanyId == command.CompanyId, cancellationToken);

        if (draft is null)
        {
            Logger.LogWarning("Reverse-sync: draft {DraftId} not found for company {CompanyId} — skipping (idempotent)",
                command.DraftId, command.CompanyId);
            Activity.Current?.SetTag("sync.draft.found", false);
            return Unit.Value;
        }

        dbSet.Remove(draft);

        // No OutboxPublisher.PublishAsync() — prevents reverse-sync → forward-sync loop
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Activity.Current?.SetTag("sync.draft.found", true);
        Logger.LogInformation("Reverse-sync: deleted draft {DraftId} for company {CompanyId}",
            command.DraftId, command.CompanyId);

        return Unit.Value;
    }
}
