using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Drafts.Save;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.Generate;

public class GenerateDraftCommand(Guid companyId, GenerateDraftRequest request) : BaseCommand<DraftResponse>
{
    public Guid CompanyId { get; } = companyId;
    public GenerateDraftRequest Request { get; } = request;
}

public class DraftGenCommandHandler(IHandlerContext context, 
    IAiPrompt<GenerateDraftRequest> aiPrompt,
    IActivityFactory activityFactory,
    IApplicationOrchestrator orchestrator,
    IChatService chatService
    ) : BaseCommandHandler(context),
    IHandler<GenerateDraftCommand, DraftResponse>
{
    public async Task<DraftResponse> HandleAsync(GenerateDraftCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("DraftGenCommand", ActivityKind.Internal);
        
        activity?.SetTag("companyId", request.CompanyId);
        activity?.SetTag("brief", request.Request.Brief);
        activity?.SetTag("prompt.name", aiPrompt.Name);
        activity?.SetTag("prompt.version", aiPrompt.Version);
        
        Logger.LogInformation("Generating draft for company {CompanyId} with brief: {Brief}", request.CompanyId, request.Request.Brief);
        var userPrompt = aiPrompt.BuildUserPrompt(request.Request);
        var systemPrompt = aiPrompt.BuildSystemPrompt();
        
        var response = await chatService.GetResponseAsync<DraftResponse>(systemPrompt, userPrompt, aiPrompt.AllowTools, cancellationToken);
        
        Logger.LogInformation("Draft generated for company {CompanyId}", request.CompanyId);

        var saveRequest = new SaveDraftRequest
        {
            Title = response.Title,
            AboutRole = response.AboutRole,
            Metadata = response.Metadata,
            Location = response.Location,
            Notes = response.Notes,
            Qualifications = response.Qualifications,
            Responsibilities = response.Responsibilities,
        };
        
        var savedDraft = await orchestrator.ExecuteCommandAsync(new SaveDraftCommand(request.CompanyId, saveRequest), cancellationToken);
        response.Id = savedDraft.Id?? string.Empty;
        
        return response;
    }
}