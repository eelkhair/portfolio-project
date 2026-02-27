using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.Save;

public class SaveDraftCommand : BaseCommand<DraftResponse>, INoTransaction
{
    public Guid CompanyId { get; set; }
    public DraftResponse Draft { get; set; } = new();
}

public class SaveDraftCommandHandler(
    IHandlerContext handlerContext,
    IAiServiceClient aiServiceClient)
    : BaseCommandHandler(handlerContext), IHandler<SaveDraftCommand, DraftResponse>
{
    public async Task<DraftResponse> HandleAsync(SaveDraftCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("company.id", command.CompanyId);
        Logger.LogInformation("Saving draft for company {CompanyId}...", command.CompanyId);

        var result = await aiServiceClient.SaveDraft(command.CompanyId, command.Draft, cancellationToken);

        Activity.Current?.SetTag("ai.draft.id", result.Id);
        Logger.LogInformation("Saved draft {DraftId} for company {CompanyId}", result.Id, command.CompanyId);

        return result;
    }
}
