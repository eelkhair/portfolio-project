using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Resumes.SectionParsed;

public class FailResumeSectionParseCommand(ResumeSectionFailedModel model) : BaseCommand<Unit>
{
    public ResumeSectionFailedModel Model { get; } = model;
}

public class FailResumeSectionParseCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<FailResumeSectionParseCommand, Unit>
{
    public async Task<Unit> HandleAsync(FailResumeSectionParseCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("FailResumeSectionParse", ActivityKind.Internal);

        var model = command.Model;
        activity?.SetTag("resume.uid", model.ResumeUId);
        activity?.SetTag("resume.section", model.Section);

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == model.ResumeUId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeUId} not found for section failure", model.ResumeUId);
            throw new NotFoundException($"Resume {model.ResumeUId} not found.");
        }

        resume.MarkSectionFailed(model.Section);

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogWarning("Resume {ResumeUId} section {Section} failed: {Reason}",
            model.ResumeUId, model.Section, model.Reason);

        return Unit.Value;
    }
}
