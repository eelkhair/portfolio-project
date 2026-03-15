using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain.Entities;
using JobBoard.IntegrationEvents.Draft;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Drafts.Save;

public class SaveDraftCommand : BaseCommand<DraftResponse>
{
    public Guid CompanyId { get; set; }
    public DraftResponse Draft { get; set; } = new();
}

public class SaveDraftCommandHandler(IHandlerContext handlerContext)
    : BaseCommandHandler(handlerContext), IHandler<SaveDraftCommand, DraftResponse>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<DraftResponse> HandleAsync(SaveDraftCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("company.id", command.CompanyId);
        Logger.LogInformation("Saving draft for company {CompanyId}...", command.CompanyId);

        var contentJson = JsonSerializer.Serialize(command.Draft, JsonOpts);
        var dbSet = ((IJobBoardQueryDbContext)Context).Drafts;

        Draft draft;
        if (!string.IsNullOrWhiteSpace(command.Draft.Id) && Guid.TryParse(command.Draft.Id, out var existingId))
        {
            draft = await dbSet
                .FirstOrDefaultAsync(d => d.Id == existingId && d.CompanyId == command.CompanyId, cancellationToken);

            if (draft is not null)
            {
                draft.SetContent(contentJson);
            }
            else
            {
                var (id, uid) = await Context.GetNextValueFromSequenceAsync(typeof(Draft), cancellationToken);
                draft = Draft.Create(command.CompanyId, contentJson, id, uid);
                dbSet.Add(draft);
            }
        }
        else
        {
            var (id, uid) = await Context.GetNextValueFromSequenceAsync(typeof(Draft), cancellationToken);
            draft = Draft.Create(command.CompanyId, contentJson, id, uid);
            dbSet.Add(draft);
        }

        var draftEvent = new DraftSavedV1Event(
            draft.Id,
            command.CompanyId,
            command.Draft.Title,
            command.Draft.AboutRole,
            command.Draft.Location,
            command.Draft.JobType,
            command.Draft.SalaryRange,
            command.Draft.Notes,
            command.Draft.Responsibilities,
            command.Draft.Qualifications
        ) { UserId = command.UserId };
        await OutboxPublisher.PublishAsync(draftEvent, cancellationToken);

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        command.Draft.Id = draft.Id.ToString();

        Activity.Current?.SetTag("ai.draft.id", draft.Id);
        Logger.LogInformation("Saved draft {DraftId} for company {CompanyId}", draft.Id, command.CompanyId);

        return command.Draft;
    }
}
