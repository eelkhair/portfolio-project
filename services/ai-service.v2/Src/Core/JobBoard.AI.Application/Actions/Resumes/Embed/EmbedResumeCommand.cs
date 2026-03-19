using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.MatchExplanations;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Drafts;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    IActivityFactory activityFactory,
    IServiceScopeFactory serviceScopeFactory) : BaseCommandHandler(context),
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

        // Build section-specific texts
        var sections = BuildSectionTexts(parsedContent);

        // Collect texts to embed: full + optional skills + optional experience
        var textsToEmbed = new List<string> { sections.Full };
        var sectionKeys = new List<string> { "full" };

        if (sections.Skills is not null) { textsToEmbed.Add(sections.Skills); sectionKeys.Add("skills"); }
        if (sections.Experience is not null) { textsToEmbed.Add(sections.Experience); sectionKeys.Add("experience"); }

        activity?.SetTag("embedding.batch.count", textsToEmbed.Count);

        // Batch embed
        var vectors = await embeddingService.GenerateBatchEmbeddingsAsync(textsToEmbed, cancellationToken);

        // Map results to named vectors
        var vectorMap = new Dictionary<string, EmbeddingVector>();
        for (var i = 0; i < sectionKeys.Count; i++)
            vectorMap[sectionKeys[i]] = new EmbeddingVector(vectors[i]);

        var provider = new ProviderName("openai.embedding");
        var model = new ModelName("text-embedding-3-small");

        var existing = await dbContext.ResumeEmbeddings
            .FirstOrDefaultAsync(e => e.ResumeUId == resumeUId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(
                vectorMap["full"], provider, model,
                skillsVector: vectorMap.GetValueOrDefault("skills"),
                experienceVector: vectorMap.GetValueOrDefault("experience"));
            activity?.SetTag("embedding.upsert", "updated");
            Logger.LogInformation("Updated existing embedding for resume {ResumeUId}", resumeUId);
        }
        else
        {
            var resumeEmbedding = new ResumeEmbedding(
                resumeUId,
                vectorMap["full"], provider, model,
                skillsVector: vectorMap.GetValueOrDefault("skills"),
                experienceVector: vectorMap.GetValueOrDefault("experience"));
            await dbContext.ResumeEmbeddings.AddAsync(resumeEmbedding, cancellationToken);
            activity?.SetTag("embedding.upsert", "created");
            Logger.LogInformation("Created new embedding for resume {ResumeUId}", resumeUId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate stale match explanations before generating new ones
        var staleExplanations = await dbContext.MatchExplanations
            .Where(e => e.ResumeUId == resumeUId)
            .ToListAsync(cancellationToken);

        if (staleExplanations.Count > 0)
        {
            dbContext.MatchExplanations.RemoveRange(staleExplanations);
            await dbContext.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("Deleted {Count} stale match explanations for resume {ResumeUId}",
                staleExplanations.Count, resumeUId);
        }

        await monolithClient.NotifyResumeEmbeddedAsync(new ResumeEmbeddedRequest
        {
            ResumeUId = resumeUId,
            UserId = request.Event.UserId
        }, cancellationToken);

        // Fire-and-forget: pre-compute match explanations for top matching jobs
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<IHandler<GenerateMatchExplanationsCommand, Unit>>();
                await handler.HandleAsync(
                    new GenerateMatchExplanationsCommand(resumeUId, request.Event.UserId),
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to generate match explanations for resume {ResumeUId}", resumeUId);
            }
        }, CancellationToken.None);

        return Unit.Value;
    }

    private record SectionTexts(string Full, string? Skills, string? Experience);

    private static SectionTexts BuildSectionTexts(ResumeParsedContentResponse content)
    {
        var skills = content.Skills.Count > 0
            ? $"Skills: {string.Join(", ", content.Skills)}"
            : null;

        var experience = content.WorkHistory.Count > 0
            ? "Work Experience:\n" + string.Join("\n", content.WorkHistory.Select(w =>
                $"- {w.JobTitle} at {w.Company}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $": {w.Description}")}"))
            : null;

        var education = content.Education.Count > 0
            ? "Education:\n" + string.Join("\n", content.Education.Select(e =>
                $"- {e.Degree}{(string.IsNullOrWhiteSpace(e.FieldOfStudy) ? "" : $" in {e.FieldOfStudy}")} at {e.Institution}"))
            : null;

        var certifications = content.Certifications.Count > 0
            ? "Certifications:\n" + string.Join("\n", content.Certifications.Select(c =>
                $"- {c.Name}{(string.IsNullOrWhiteSpace(c.IssuingOrganization) ? "" : $" ({c.IssuingOrganization})")}"))
            : null;

        var projects = content.Projects.Count > 0
            ? "Projects:\n" + string.Join("\n", content.Projects.Select(p =>
                $"- {p.Name}{(string.IsNullOrWhiteSpace(p.Description) ? "" : $": {p.Description}")}" +
                (p.Technologies.Count > 0 ? $" [{string.Join(", ", p.Technologies)}]" : "")))
            : null;

        var summary = !string.IsNullOrWhiteSpace(content.Summary)
            ? content.Summary
            : null;

        // Full text concatenates all sections for broad semantic coverage
        var full = $"""
                Summary:
                {summary ?? "N/A"}

                Skills:
                {skills ?? "N/A"}

                Work History:
                {experience ?? "N/A"}

                Education:
                {education ?? "N/A"}

                Certifications:
                {certifications ?? "N/A"}

                Projects:
                {projects ?? "N/A"}
                """;

        return new SectionTexts(full, skills, experience);
    }
}
