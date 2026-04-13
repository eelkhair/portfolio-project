using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.IntegrationEvents.Resume;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.SectionParsed;

public class CompleteAllSectionsCommand(ResumeAllSectionsCompletedModel model) : BaseCommand<Unit>
{
    public ResumeAllSectionsCompletedModel Model { get; } = model;
}

public class CompleteAllSectionsCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<CompleteAllSectionsCommand, Unit>
{
    public async Task<Unit> HandleAsync(CompleteAllSectionsCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("CompleteAllSections", ActivityKind.Internal);

        var model = command.Model;
        activity?.SetTag("resume.uid", model.ResumeUId);

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == model.ResumeUId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeUId} not found for all-sections completion", model.ResumeUId);
            throw new NotFoundException($"Resume {model.ResumeUId} not found.");
        }

        if (resume.AreAllSectionsComplete())
        {
            resume.MarkFullyParsed();

            var integrationEvent = new ResumeParsedV1Event(ResumeUId: model.ResumeUId)
            {
                UserId = command.UserId
            };

            await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);

            Logger.LogInformation("Resume {ResumeUId} all sections complete, publishing ResumeParsedV1Event",
                model.ResumeUId);
        }
        else
        {
            Logger.LogWarning("Resume {ResumeUId} all-sections-completed called but not all sections resolved. " +
                              "Parsed: {Parsed}, Failed: {Failed}",
                model.ResumeUId, resume.ParsedSections, resume.FailedSections);
        }

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        return Unit.Value;
    }
}
