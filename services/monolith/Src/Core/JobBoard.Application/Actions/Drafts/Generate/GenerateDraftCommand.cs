using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.Generate;

public class GenerateDraftCommand : BaseCommand<DraftGenResponse>
{
    public Guid CompanyId { get; set; }
    public DraftGenRequest Request { get; set; } = new();
}

public class GenerateDraftCommandHandler(
    IHandlerContext handlerContext,
    IAiServiceClient aiServiceClient)
    : BaseCommandHandler(handlerContext), IHandler<GenerateDraftCommand, DraftGenResponse>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DraftGenResponse> HandleAsync(GenerateDraftCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("company.id", command.CompanyId);
        Logger.LogInformation("Generating draft for company {CompanyId}...", command.CompanyId);

        var result = await aiServiceClient.GenerateDraft(command.CompanyId, command.Request, cancellationToken);

        Activity.Current?.SetTag("ai.draft.title", result.Title);
        Logger.LogInformation("LLM generated draft content for company {CompanyId}, saving locally...", command.CompanyId);

        // Save the generated draft locally
        var draftContent = new DraftResponse
        {
            Title = result.Title,
            AboutRole = result.AboutRole,
            Responsibilities = result.Responsibilities,
            Qualifications = result.Qualifications,
            Notes = result.Notes,
            Location = result.Location,
            Metadata = result.Metadata
        };

        var contentJson = JsonSerializer.Serialize(draftContent, JsonOpts);
        var dbSet = ((IJobBoardQueryDbContext)Context).Drafts;

        var (id, uid) = await Context.GetNextValueFromSequenceAsync(typeof(Draft), cancellationToken);
        var draft = Draft.Create(command.CompanyId, contentJson, id, uid);
        dbSet.Add(draft);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        result.DraftId = draft.Id.ToString();

        Activity.Current?.SetTag("ai.draft.id", draft.Id);
        Logger.LogInformation("Generated and saved draft {DraftId} for company {CompanyId}", draft.Id, command.CompanyId);

        return result;
    }
}
