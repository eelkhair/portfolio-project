using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Sync;

public class SyncDraftSaveCommand : BaseCommand<Unit>
{
    public Guid DraftId { get; set; }
    public Guid CompanyId { get; set; }
    public string ContentJson { get; set; } = "{}";
}

/// <summary>
/// Reverse-sync handler: upserts a draft from a microservice event.
/// Does NOT call OutboxPublisher to prevent infinite sync loops.
/// Follows the same pattern as <see cref="Companies.Activate.ActivateCompanyCommandHandler"/>.
/// </summary>
public class SyncDraftSaveCommandHandler(IHandlerContext handlerContext)
    : BaseCommandHandler(handlerContext), IHandler<SyncDraftSaveCommand, Unit>
{
    public async Task<Unit> HandleAsync(SyncDraftSaveCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("sync.draft.id", command.DraftId);
        Activity.Current?.SetTag("sync.draft.companyId", command.CompanyId);
        Logger.LogInformation("Reverse-sync: saving draft {DraftId} for company {CompanyId}",
            command.DraftId, command.CompanyId);

        var dbSet = ((IJobBoardQueryDbContext)Context).Drafts;

        var draft = await dbSet
            .FirstOrDefaultAsync(d => d.Id == command.DraftId && d.CompanyId == command.CompanyId, cancellationToken);

        if (draft is not null)
        {
            // Update existing draft
            draft.SetContent(command.ContentJson);
            Activity.Current?.SetTag("sync.draft.isNew", false);
        }
        else
        {
            // Create new draft with the microservice's GUID
            var (internalId, _) = await Context.GetNextValueFromSequenceAsync(typeof(Draft), cancellationToken);
            draft = Draft.Create(command.CompanyId, command.ContentJson, internalId, command.DraftId);
            dbSet.Add(draft);
            Activity.Current?.SetTag("sync.draft.isNew", true);
        }

        // No OutboxPublisher.PublishAsync() — prevents reverse-sync → forward-sync loop
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogInformation("Reverse-sync: saved draft {DraftId} for company {CompanyId}",
            command.DraftId, command.CompanyId);

        return Unit.Value;
    }
}
