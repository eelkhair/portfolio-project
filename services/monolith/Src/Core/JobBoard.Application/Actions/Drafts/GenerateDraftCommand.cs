using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts;

public class GenerateDraftCommand : BaseCommand<DraftGenResponse>, INoTransaction
{
    public Guid CompanyId { get; set; }
    public DraftGenRequest Request { get; set; } = new();
}

public class GenerateDraftCommandHandler(
    IHandlerContext handlerContext,
    IAiServiceClient aiServiceClient)
    : BaseCommandHandler(handlerContext), IHandler<GenerateDraftCommand, DraftGenResponse>
{
    public async Task<DraftGenResponse> HandleAsync(GenerateDraftCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("company.id", command.CompanyId);
        Logger.LogInformation("Generating draft for company {CompanyId}...", command.CompanyId);

        var result = await aiServiceClient.GenerateDraft(command.CompanyId, command.Request, cancellationToken);

        Activity.Current?.SetTag("ai.draft.id", result.DraftId);
        Activity.Current?.SetTag("ai.draft.title", result.Title);
        Logger.LogInformation("Generated draft {DraftId} for company {CompanyId}", result.DraftId, command.CompanyId);

        return result;
    }
}
