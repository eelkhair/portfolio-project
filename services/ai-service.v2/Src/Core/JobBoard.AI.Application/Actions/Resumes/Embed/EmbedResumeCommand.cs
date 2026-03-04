using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Application.Interfaces.Resumes;
using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Drafts;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.Embed;

public class EmbedResumeCommand(EventDto<ResumeParsedV1Event> @event) : BaseCommand<Unit>, ISystemCommand
{
    public EventDto<ResumeParsedV1Event> Event { get; set; } = @event;
}

public class EmbedResumeCommandHandler(
    IHandlerContext context,
    IMonolithApiClient monolithClient,
    IAiDbContext dbContext,
    IEmbeddingService embeddingService,
    IActivityFactory activityFactory) : BaseCommandHandler(context),
    IHandler<EmbedResumeCommand, Unit>
{
    public async Task<Unit> HandleAsync(EmbedResumeCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("EmbedResumeCommandHandler.HandleAsync", ActivityKind.Internal);

        var resumeUId = request.Event.Data.ResumeUId;
        activity?.SetTag("resume.uid", resumeUId);

        Logger.LogInformation("Processing resume embedding for {ResumeUId}", resumeUId);

        var parsedContent = await monolithClient.GetResumeParsedContentAsync(resumeUId, cancellationToken);

        if (parsedContent is null)
        {
            Logger.LogWarning("No parsed content found for resume {ResumeUId}, skipping embedding", resumeUId);
            return Unit.Value;
        }

        var embeddingText = BuildEmbeddingText(parsedContent);
        activity?.SetTag("embedding.text.length", embeddingText.Length);

        var vector = await embeddingService.GenerateEmbeddingsAsync(embeddingText, cancellationToken);

        var embeddingVector = new EmbeddingVector(vector);
        var provider = new ProviderName("openai.embedding");
        var model = new ModelName("text-embedding-3-small");

        var existing = await dbContext.ResumeEmbeddings
            .FirstOrDefaultAsync(e => e.ResumeUId == resumeUId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(embeddingVector, provider, model);
            activity?.SetTag("embedding.upsert", "updated");
            Logger.LogInformation("Updated existing embedding for resume {ResumeUId}", resumeUId);
        }
        else
        {
            var resumeEmbedding = new ResumeEmbedding(resumeUId, embeddingVector, provider, model);
            await dbContext.ResumeEmbeddings.AddAsync(resumeEmbedding, cancellationToken);
            activity?.SetTag("embedding.upsert", "created");
            Logger.LogInformation("Created new embedding for resume {ResumeUId}", resumeUId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private static string BuildEmbeddingText(ResumeParsedContentResponse content)
    {
        var skills = content.Skills.Count > 0
            ? string.Join(", ", content.Skills)
            : "N/A";

        var workHistory = content.WorkHistory.Count > 0
            ? string.Join("\n", content.WorkHistory.Select(w =>
                $"- {w.JobTitle} at {w.Company}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $": {w.Description}")}"))
            : "N/A";

        var education = content.Education.Count > 0
            ? string.Join("\n", content.Education.Select(e =>
                $"- {e.Degree}{(string.IsNullOrWhiteSpace(e.FieldOfStudy) ? "" : $" in {e.FieldOfStudy}")} at {e.Institution}"))
            : "N/A";

        var certifications = content.Certifications.Count > 0
            ? string.Join("\n", content.Certifications.Select(c =>
                $"- {c.Name}{(string.IsNullOrWhiteSpace(c.IssuingOrganization) ? "" : $" ({c.IssuingOrganization})")}"))
            : "N/A";

        return $"""
                Skills:
                {skills}

                Work History:
                {workHistory}

                Education:
                {education}

                Certifications:
                {certifications}
                """;
    }
}
