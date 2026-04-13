using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.MatchingJobs;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector.EntityFrameworkCore;

namespace JobBoard.AI.Application.Actions.Resumes.MatchExplanations;

public class GenerateMatchExplanationsCommand(Guid resumeUId, string? userId = null) : BaseCommand<Unit>, ISystemCommand
{
    public Guid ResumeUId { get; } = resumeUId;
    public new string? UserId { get; } = userId;
}

public class GenerateMatchExplanationsCommandHandler(
    IHandlerContext context,
    IAiDbContext dbContext,
    IMonolithApiClient monolithClient,
    IChatService chatService,
    IActivityFactory activityFactory)
    : BaseCommandHandler(context), IHandler<GenerateMatchExplanationsCommand, Unit>
{
    private const int TopN = 10;

    private const string SystemPrompt = """
        You are a job matching analyst. Given a user's resume and a list of job postings,
        explain why each job matches their profile. Address the user directly using "you" and "your".

        Each job includes a similarity score (0-100%). Calibrate your language to match the score:
        - 80-100%: strong/excellent match
        - 60-79%: good match with some gaps
        - 40-59%: partial match, highlight what aligns and what doesn't
        - Below 40%: limited match, be honest about gaps
        Never say "perfectly matches" unless the score is above 90%.

        For each job, provide:
        1. A "summary" — a concise 1-2 sentence explanation calibrated to the match score.
        2. A "details" array — 3-5 bullet points highlighting specific skill overlaps,
           relevant experience, or qualification alignment.
        3. A "gaps" array — 1-3 bullet points describing what the candidate is missing
           or where their profile doesn't align. Be specific (e.g. "No experience with Kubernetes"
           not "Missing some skills"). If the match score is above 85%, gaps can be empty.

        Focus on concrete matches: specific skills, technologies, years of experience,
        domain knowledge, education, and certifications that align.

        Respond with valid JSON matching this schema exactly:
        {
          "explanations": [
            {
              "jobId": "guid-here",
              "summary": "Short explanation of match...",
              "details": ["Bullet point 1", "Bullet point 2", "..."],
              "gaps": ["Gap point 1", "Gap point 2"]
            }
          ]
        }
        """;

    public async Task<Unit> HandleAsync(GenerateMatchExplanationsCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "GenerateMatchExplanationsCommandHandler.HandleAsync", ActivityKind.Internal);

        var resumeUId = request.ResumeUId;
        activity?.SetTag("resume.uid", resumeUId);

        // 1. Get the resume embedding to find matching jobs
        var resume = await dbContext.ResumeEmbeddings
            .FirstOrDefaultAsync(e => e.ResumeUId == resumeUId, cancellationToken);

        if (resume is null)
        {
            Logger.LogWarning("No embedding found for resume {ResumeUId}, skipping explanation generation", resumeUId);
            return Unit.Value;
        }

        // 2. Calculate top N matching jobs (reuse weighted similarity logic)
        var matchingJobs = await GetTopMatchingJobs(resume, cancellationToken);

        if (matchingJobs.Count == 0)
        {
            Logger.LogInformation("No matching jobs found for resume {ResumeUId}", resumeUId);
            return Unit.Value;
        }

        var matchingJobIds = matchingJobs.Select(m => m.JobId).ToList();
        var scoresByJobId = matchingJobs.ToDictionary(m => m.JobId, m => m.Score);

        // 3. Check which explanations already exist (cache hit)
        var existingExplanations = await dbContext.MatchExplanations
            .Where(e => e.ResumeUId == resumeUId && matchingJobIds.Contains(e.JobId))
            .Select(e => e.JobId)
            .ToListAsync(cancellationToken);

        var uncachedJobIds = matchingJobIds.Except(existingExplanations).ToList();

        activity?.SetTag("explanation.cached_count", existingExplanations.Count);
        activity?.SetTag("explanation.uncached_count", uncachedJobIds.Count);

        if (uncachedJobIds.Count == 0)
        {
            Logger.LogInformation("All explanations cached for resume {ResumeUId}", resumeUId);
            return Unit.Value;
        }

        // 4. Fetch resume parsed content
        var parsedContent = await monolithClient.GetResumeParsedContentAsync(resumeUId, cancellationToken);
        if (parsedContent is null)
        {
            Logger.LogWarning("No parsed content for resume {ResumeUId}, skipping explanations", resumeUId);
            return Unit.Value;
        }

        // 5. Fetch all job details upfront
        var jobDetails = await monolithClient.GetJobsBatchAsync(uncachedJobIds, cancellationToken);
        if (jobDetails.Count == 0)
        {
            Logger.LogWarning("No job details returned for batch request, skipping explanations");
            return Unit.Value;
        }

        // Sort by similarity score descending
        var orderedJobDetails = jobDetails
            .OrderByDescending(j => scoresByJobId.GetValueOrDefault(j.JobId, 0))
            .ToList();

        // 6. Split into batches and generate explanations in parallel
        const int batchSize = 3;
        var batches = orderedJobDetails.Chunk(batchSize).ToList();

        var sw = Stopwatch.StartNew();
        var llmTasks = batches.Select(batch =>
            CallLlmForBatchAsync(parsedContent, batch.ToList(), scoresByJobId, cancellationToken));
        var llmResults = await Task.WhenAll(llmTasks);
        sw.Stop();

        var allExplanations = llmResults
            .Where(r => r is not null)
            .SelectMany(r => r!.Explanations)
            .ToList();

        Logger.LogInformation(
            "Generated {Count} match explanations for resume {ResumeUId} in {Duration}ms ({BatchCount} parallel batches)",
            allExplanations.Count, resumeUId, sw.ElapsedMilliseconds, batches.Count);

        // 7. Persist all results in a single DB operation
        await PersistExplanationsAsync(resumeUId, allExplanations, scoresByJobId, cancellationToken);
        await NotifyFrontendAsync(resumeUId, request.UserId, cancellationToken);

        activity?.SetTag("explanation.generated_count", allExplanations.Count);

        return Unit.Value;
    }

    private async Task<MatchExplanationLlmResponse?> CallLlmForBatchAsync(
        ResumeParsedContentResponse parsedContent,
        List<JobBatchDetailDto> jobs,
        Dictionary<Guid, double> scoresByJobId,
        CancellationToken ct)
    {
        var userPrompt = BuildUserPrompt(parsedContent, jobs, scoresByJobId);
        try
        {
            return await chatService.GetResponseAsync<MatchExplanationLlmResponse>(
                SystemPrompt, userPrompt, allowTools: false, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "LLM batch call failed for {Count} jobs", jobs.Count);
            return null;
        }
    }

    private async Task PersistExplanationsAsync(
        Guid resumeUId,
        List<JobExplanationItem> explanations,
        Dictionary<Guid, double> scoresByJobId,
        CancellationToken ct)
    {
        if (explanations.Count == 0) return;

        var jobIds = explanations.Select(e => e.JobId).ToList();
        var existingByJobId = await dbContext.MatchExplanations
            .Where(e => e.ResumeUId == resumeUId && jobIds.Contains(e.JobId))
            .ToDictionaryAsync(e => e.JobId, ct);

        foreach (var item in explanations)
        {
            var detailsJson = JsonSerializer.Serialize(item.Details);
            var gapsJson = JsonSerializer.Serialize(item.Gaps);
            var similarity = scoresByJobId.GetValueOrDefault(item.JobId, 0);

            if (existingByJobId.TryGetValue(item.JobId, out var existing))
            {
                existing.Update(item.Summary, detailsJson, gapsJson, "openai", "gpt-4o-mini", similarity);
            }
            else
            {
                var explanation = new MatchExplanation(
                    resumeUId, item.JobId, item.Summary, detailsJson, gapsJson, "openai", "gpt-4o-mini", similarity);
                await dbContext.MatchExplanations.AddAsync(explanation, ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task NotifyFrontendAsync(Guid resumeUId, string? userId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            await monolithClient.NotifyMatchExplanationsGeneratedAsync(
                new MatchExplanationsGeneratedRequest
                {
                    ResumeUId = resumeUId,
                    UserId = userId
                }, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send MatchExplanationsGenerated notification for resume {ResumeUId}", resumeUId);
        }
    }

    private async Task<List<(Guid JobId, double Score)>> GetTopMatchingJobs(ResumeEmbedding resume, CancellationToken ct)
    {
        var hasSections = resume.SkillsVectorData is not null || resume.ExperienceVectorData is not null;

        if (!hasSections)
        {
            // Simple cosine similarity — computed in pgvector
            var raw = await dbContext.JobEmbeddings
                .Select(j => new { j.JobId, Similarity = 1 - j.VectorData.CosineDistance(resume.VectorData) })
                .OrderByDescending(x => x.Similarity)
                .Take(TopN)
                .ToListAsync(ct);
            return raw.Select(r => (r.JobId, r.Similarity)).ToList();
        }

        // Weighted composite similarity — reuse shared DB query from ListMatchingJobsQueryHandler
        var (fullWeight, skillsW, experienceW) = ListMatchingJobsQueryHandler.ComputeWeights(resume);
        var scored = await ListMatchingJobsQueryHandler.ComputeWeightedScoresInDb(
            dbContext, resume, fullWeight, skillsW, experienceW, TopN, ct);
        return scored.Select(s => (s.JobId, s.Score)).ToList();
    }

    private static string BuildUserPrompt(ResumeParsedContentResponse resume, List<JobBatchDetailDto> jobs, Dictionary<Guid, double> scoresByJobId)
    {
        var resumeText = $"""
            ## Candidate Resume

            Summary: {resume.Summary ?? "N/A"}

            Skills: {(resume.Skills.Count > 0 ? string.Join(", ", resume.Skills) : "N/A")}

            Work Experience:
            {(resume.WorkHistory.Count > 0
                ? string.Join("\n", resume.WorkHistory.Select(w =>
                    $"- {w.JobTitle} at {w.Company}{(string.IsNullOrWhiteSpace(w.Description) ? "" : $": {w.Description}")}"))
                : "N/A")}

            Education:
            {(resume.Education.Count > 0
                ? string.Join("\n", resume.Education.Select(e =>
                    $"- {e.Degree}{(string.IsNullOrWhiteSpace(e.FieldOfStudy) ? "" : $" in {e.FieldOfStudy}")} at {e.Institution}"))
                : "N/A")}

            Certifications:
            {(resume.Certifications.Count > 0
                ? string.Join("\n", resume.Certifications.Select(c =>
                    $"- {c.Name}{(string.IsNullOrWhiteSpace(c.IssuingOrganization) ? "" : $" ({c.IssuingOrganization})")}"))
                : "N/A")}
            """;

        var jobsText = string.Join("\n\n", jobs.Select(j =>
        {
            var scorePercent = scoresByJobId.TryGetValue(j.JobId, out var s)
                ? (int)(ListMatchingJobsQueryHandler.NormalizeScore(s) * 100) : 0;
            return $"""
            ---
            Job ID: {j.JobId}
            Title: {j.Title}
            Match Score: {scorePercent}%
            Location: {j.Location ?? "N/A"}
            Type: {j.JobType ?? "N/A"}
            About: {j.AboutRole ?? "N/A"}
            Responsibilities: {(j.Responsibilities.Count > 0 ? string.Join("; ", j.Responsibilities) : "N/A")}
            Qualifications: {(j.Qualifications.Count > 0 ? string.Join("; ", j.Qualifications) : "N/A")}
            """;
        }));

        return $"""
            {resumeText}

            ## Job Postings

            {jobsText}
            """;
    }
}
