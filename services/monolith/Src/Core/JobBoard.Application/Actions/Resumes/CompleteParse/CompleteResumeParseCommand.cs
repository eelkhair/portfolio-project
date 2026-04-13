using System.Diagnostics;
using System.Text.Json;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.CompleteParse;

public class CompleteResumeParseCommand(ResumeParseCompletedModel model) : BaseCommand<Unit>
{
    public ResumeParseCompletedModel Model { get; } = model;
}

public class CompleteResumeParseCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<CompleteResumeParseCommand, Unit>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Unit> HandleAsync(CompleteResumeParseCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("CompleteResumeParse", ActivityKind.Internal);

        var model = command.Model;
        activity?.SetTag("resume.uid", model.ResumeUId);

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == model.ResumeUId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeUId} not found for parse completion", model.ResumeUId);
            throw new NotFoundException($"Resume {model.ResumeUId} not found.");
        }

        var json = JsonSerializer.Serialize(model.ParsedContent, JsonOpts);
        resume.MarkParsed(json);

        var integrationEvent = new ResumeParsedV1Event(ResumeUId: model.ResumeUId)
        {
            UserId = command.UserId
        };

        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            Logger.LogInformation("Resume {ResumeUId} parsing completed successfully", model.ResumeUId);
            Activity.Current?.SetTag("status", "completed");
            return Task.CompletedTask;
        });

        return Unit.Value;
    }
}
