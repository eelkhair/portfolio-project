using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Drafts.Save;

public class SaveDraftCommand(Guid companyId, SaveDraftRequest request) : BaseCommand<SaveDraftResponse>
{
    public SaveDraftRequest Request { get; } = request;
    public Guid CompanyId { get; } = companyId;
}

public class SaveDraftCommandHandler(IHandlerContext handlerContext, 
    IAiDbContext context,
    IActivityFactory activityFactory) : BaseCommandHandler(handlerContext),
    IHandler<SaveDraftCommand, SaveDraftResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    public async Task<SaveDraftResponse> HandleAsync(SaveDraftCommand command, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Saving Draft for company {CompanyId} to database",  command.CompanyId);
        
        using var activity = activityFactory.StartActivity("SaveDraftCommandHandler.HandleAsync", ActivityKind.Internal);
        activity?.SetTag("companyId", command.CompanyId);
       

        var req = command.Request;

        var contentJson = JsonSerializer.Serialize(req, JsonOptions);

        Draft draft;

        if (!string.IsNullOrWhiteSpace(req.Id) &&
            Guid.TryParse(req.Id, out var draftId))
        {
            draft = await context.Drafts
                        .SingleOrDefaultAsync(x => x.Id == draftId && x.CompanyId == command.CompanyId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Draft '{draftId}' not found.");
        }
        else
        {
            draft = new Draft(command.CompanyId, DraftType.Job);
 
            await context.Drafts.AddAsync(draft, cancellationToken);
        }

        draft.SetContent(contentJson);

    
        await context.SaveChangesAsync(cancellationToken: cancellationToken);
        
        Logger.LogInformation("Draft {DraftId} saved successfully", draft.Id);
        activity?.SetTag("draftId", draft.Id);
        return new SaveDraftResponse
        {
            Id = draft.Id.ToString(),
            Title = req.Title,
            AboutRole = req.AboutRole,
            Responsibilities = req.Responsibilities,
            Qualifications = req.Qualifications,
            Notes = req.Notes,
            Location = req.Location,
            JobType = req.JobType,
            SalaryRange = req.SalaryRange,
            Metadata = req.Metadata
        };
    }
}