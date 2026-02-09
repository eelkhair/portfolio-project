using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.Generate;

public class DraftGenCommand(Guid companyId, DraftGenRequest request) : BaseCommand<DraftGenResponse>
{
    public Guid CompanyId { get; } = companyId;
    public DraftGenRequest Request { get; } = request;
}

public class DraftGenCommandHandler(IHandlerContext context, 
    IAiPrompt<DraftGenRequest> aiPrompt,
    IActivityFactory activityFactory,
    ICompletionService completionService
    ) : BaseCommandHandler(context),
    IHandler<DraftGenCommand, DraftGenResponse>
{
    public async Task<DraftGenResponse> HandleAsync(DraftGenCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("DraftGenCommand", ActivityKind.Internal);
        
        activity?.SetTag("companyId", request.CompanyId);
        activity?.SetTag("brief", request.Request.Brief);
        activity?.SetTag("prompt.name", aiPrompt.Name);
        activity?.SetTag("prompt.version", aiPrompt.Version);
        
        Logger.LogInformation("Generating draft for company {CompanyId} with brief: {Brief}", request.CompanyId, request.Request.Brief);
        var userPrompt = aiPrompt.BuildUserPrompt(request.Request);
        var systemPrompt = aiPrompt.BuildSystemPrompt();
        
        var response = await completionService.GetResponseAsync<DraftGenResponse>(systemPrompt, userPrompt, cancellationToken);
        
        return response;
    }
}