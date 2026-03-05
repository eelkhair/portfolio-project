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

public class CompleteResumeSectionParseCommand(ResumeSectionParsedModel model) : BaseCommand<Unit>
{
    public ResumeSectionParsedModel Model { get; } = model;
}

public class CompleteResumeSectionParseCommandHandler(
    IHandlerContext context,
    IActivityFactory activityFactory,
    IJobBoardDbContext db)
    : BaseCommandHandler(context),
      IHandler<CompleteResumeSectionParseCommand, Unit>
{
    public async Task<Unit> HandleAsync(CompleteResumeSectionParseCommand command, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("CompleteResumeSectionParse", ActivityKind.Internal);

        var model = command.Model;
        activity?.SetTag("resume.uid", model.ResumeUId);
        activity?.SetTag("resume.section", model.Section);

        var resume = await db.Resumes
            .FirstOrDefaultAsync(r => r.Id == model.ResumeUId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("Resume {ResumeUId} not found for section parse", model.ResumeUId);
            throw new NotFoundException($"Resume {model.ResumeUId} not found.");
        }

        var sectionJson = model.SectionContent.GetRawText();
        resume.MergeSectionContent(model.Section, sectionJson);

        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogInformation("Resume {ResumeUId} section {Section} parsed successfully",
            model.ResumeUId, model.Section);

        return Unit.Value;
    }
}
