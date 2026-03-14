using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.Delete;

public class DeleteDraftCommand : BaseCommand<bool>
{
    public Guid DraftId { get; set; }
    public Guid CompanyId { get; set; }
}

public class DeleteDraftCommandHandler(IHandlerContext handlerContext)
    : BaseCommandHandler(handlerContext), IHandler<DeleteDraftCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteDraftCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("draft.id", command.DraftId);
        Activity.Current?.SetTag("company.id", command.CompanyId);
        Logger.LogInformation("Deleting draft {DraftId} for company {CompanyId}...", command.DraftId, command.CompanyId);

        var dbSet = ((IJobBoardQueryDbContext)Context).Drafts;
        var draft = await dbSet
            .FirstOrDefaultAsync(d => d.Id == command.DraftId && d.CompanyId == command.CompanyId, cancellationToken);

        if (draft is null)
            throw new DomainException("Draft.NotFound",
                [new Error("Draft.NotFound", $"Draft '{command.DraftId}' not found for company '{command.CompanyId}'.")]);

        dbSet.Remove(draft);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogInformation("Deleted draft {DraftId} for company {CompanyId}", command.DraftId, command.CompanyId);
        return true;
    }
}
