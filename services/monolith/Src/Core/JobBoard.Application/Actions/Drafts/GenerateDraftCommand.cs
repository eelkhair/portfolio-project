using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts;

public class GenerateDraftCommand : BaseCommand<JobGenResponse>, INoTransaction
{
    public Guid CompanyId { get; set; }
    public JobGenRequest Request { get; set; } = new();
}

public class GenerateDraftCommandHandler(
    IHandlerContext handlerContext,
    IAiServiceClient aiServiceClient)
    : BaseCommandHandler(handlerContext), IHandler<GenerateDraftCommand, JobGenResponse>
{
    public async Task<JobGenResponse> HandleAsync(GenerateDraftCommand command, CancellationToken cancellationToken)
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
