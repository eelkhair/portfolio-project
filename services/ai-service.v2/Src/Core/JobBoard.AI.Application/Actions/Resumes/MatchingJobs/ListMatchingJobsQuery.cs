using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace JobBoard.AI.Application.Actions.Resumes.MatchingJobs;

public class ListMatchingJobsQuery(Guid resumeId, int limit = 10) : BaseQuery<List<JobCandidate>>, ISystemCommand
{
    public int Limit { get; } = limit;
    public Guid ResumeId { get; } = resumeId;
}

public class ListMatchingJobsQueryHandler(
    IAiDbContext context,
    IActivityFactory activityFactory,
    ILogger<ListMatchingJobsQueryHandler> logger)
    : BaseQueryHandler(logger), IHandler<ListMatchingJobsQuery, List<JobCandidate>>
{
    private const double WFull = 0.20;
    private const double WSkills = 0.40;
    private const double WExperience = 0.40;

    public async Task<List<JobCandidate>> HandleAsync(ListMatchingJobsQuery request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ListMatchingJobsQueryHandler.HandleAsync", ActivityKind.Internal);
        activity?.SetTag("resume.uid", request.ResumeId);
        activity?.SetTag("matching.limit", request.Limit);

        // Try cached path first: if explanations with scores exist, skip vector recomputation
        try
        {
            var cached = await context.MatchExplanations
                .Where(e => e.ResumeUId == request.ResumeId && e.Similarity > 0)
                .OrderByDescending(e => e.Similarity)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            if (cached.Count >= request.Limit)
            {
                activity?.SetTag("matching.mode", "cached");
                activity?.SetTag("matching.count", cached.Count);
                activity?.SetTag("matching.explanations_found", cached.Count);

                return cached.Select((e, i) => new JobCandidate
                {
                    JobId = e.JobId,
                    Similarity = NormalizeScore(e.Similarity),
                    Rank = i + 1,
                    MatchSummary = e.Summary,
                    MatchDetails = JsonSerializer.Deserialize<List<string>>(e.DetailsJson),
                    MatchGaps = JsonSerializer.Deserialize<List<string>>(e.GapsJson)
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check cached explanations, falling back to vector query");
        }

        var resume = await context.ResumeEmbeddings
            .FirstOrDefaultAsync(e => e.ResumeUId == request.ResumeId, cancellationToken);

        if (resume is null)
        {
            activity?.SetTag("matching.mode", "no_embedding");
            logger.LogInformation("Resume {ResumeId} has no embedding yet — returning empty matches", request.ResumeId);
            return [];
        }

        var hasSections = resume.SkillsVectorData is not null || resume.ExperienceVectorData is not null;

        List<JobCandidate> results;

        if (!hasSections)
        {
            activity?.SetTag("matching.mode", "simple");
            results = await SimpleCosineSimilarity(resume.VectorData, request.Limit, cancellationToken);
        }
        else
        {
            activity?.SetTag("matching.mode", "weighted");
            results = await WeightedCompositeSimilarity(resume, request.Limit, cancellationToken);
        }

        // Merge cached match explanations (graceful — skip if table doesn't exist yet)
        try
        {
            var jobIds = results.Select(r => r.JobId).ToList();
            var explanations = await context.MatchExplanations
                .Where(e => e.ResumeUId == request.ResumeId && jobIds.Contains(e.JobId))
                .ToDictionaryAsync(e => e.JobId, cancellationToken);

            foreach (var result in results)
            {
                if (explanations.TryGetValue(result.JobId, out var explanation))
                {
                    result.MatchSummary = explanation.Summary;
                    result.MatchDetails = JsonSerializer.Deserialize<List<string>>(explanation.DetailsJson);
                    result.MatchGaps = JsonSerializer.Deserialize<List<string>>(explanation.GapsJson);
                }
            }

            activity?.SetTag("matching.explanations_found", explanations.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load match explanations, returning results without explanations");
            activity?.SetTag("matching.explanations_error", true);
        }

        activity?.SetTag("matching.count", results.Count);
        return results;
    }

    private async Task<List<JobCandidate>> SimpleCosineSimilarity(Vector resumeVector, int limit, CancellationToken ct)
    {
        var raw = await context.JobEmbeddings
            .OrderByDescending(e => 1 - e.VectorData.CosineDistance(resumeVector))
            .Select(c => new { c.JobId, Similarity = 1 - c.VectorData.CosineDistance(resumeVector) })
            .Take(limit)
            .ToListAsync(ct);

        var results = raw.Select((r, i) => new JobCandidate
        {
            JobId = r.JobId,
            Similarity = NormalizeScore(r.Similarity),
            Rank = i + 1
        }).ToList();

        return results;
    }

    private async Task<List<JobCandidate>> WeightedCompositeSimilarity(
        ResumeEmbedding resume, int limit, CancellationToken ct)
    {
        var (fullWeight, skillsW, experienceW) = ComputeWeights(resume);

        // Compute per-dimension similarities in pgvector, score and sort in DB
        var scored = await ComputeWeightedScoresInDb(context, resume, fullWeight, skillsW, experienceW, limit, ct);

        var results = new List<JobCandidate>(scored.Count);
        for (var i = 0; i < scored.Count; i++)
        {
            var s = scored[i];
            results.Add(new JobCandidate { JobId = s.JobId, Similarity = NormalizeScore(s.Score), Rank = i + 1 });

            var p = $"matching.result.{i + 1}";
            Activity.Current?.SetTag($"{p}.job_id", s.JobId);
            Activity.Current?.SetTag($"{p}.score", F(s.Score));
            Activity.Current?.SetTag($"{p}.full", SW(s.FullSim, fullWeight));
            if (s.SkillsSim.HasValue) Activity.Current?.SetTag($"{p}.skills", SW(s.SkillsSim.Value, skillsW));
            if (s.ExperienceSim.HasValue) Activity.Current?.SetTag($"{p}.experience", SW(s.ExperienceSim.Value, experienceW));
        }

        return results;
    }

    internal static (double fullWeight, double skillsW, double experienceW) ComputeWeights(ResumeEmbedding resume)
    {
        var skillsW = resume.SkillsVectorData is not null ? WSkills : 0.0;
        var experienceW = resume.ExperienceVectorData is not null ? WExperience : 0.0;

        var usedSectionWeight = skillsW + experienceW;
        var fullWeight = WFull + (1.0 - WFull - usedSectionWeight);

        var total = fullWeight + usedSectionWeight;
        fullWeight /= total;
        skillsW /= total;
        experienceW /= total;

        return (fullWeight, skillsW, experienceW);
    }

    internal record ScoredJob(Guid JobId, double Score, double FullSim, double? SkillsSim, double? ExperienceSim);

    internal static async Task<List<ScoredJob>> ComputeWeightedScoresInDb(
        IAiDbContext context,
        ResumeEmbedding resume, double fullWeight, double skillsW, double experienceW,
        int limit, CancellationToken ct)
    {
        var hasSkills = resume.SkillsVectorData is not null;
        var hasExperience = resume.ExperienceVectorData is not null;

        // Use pgvector CosineDistance in DB for each dimension, then combine with weights
        var query = context.JobEmbeddings
            .Select(j => new
            {
                j.JobId,
                FullSim = 1 - j.VectorData.CosineDistance(resume.VectorData),
                SkillsSim = hasSkills ? (double?)(1 - j.VectorData.CosineDistance(resume.SkillsVectorData!)) : null,
                ExperienceSim = hasExperience ? (double?)(1 - j.VectorData.CosineDistance(resume.ExperienceVectorData!)) : null,
            })
            .Select(x => new
            {
                x.JobId,
                x.FullSim,
                x.SkillsSim,
                x.ExperienceSim,
                Score = fullWeight * x.FullSim
                    + skillsW * (x.SkillsSim ?? 0)
                    + experienceW * (x.ExperienceSim ?? 0)
            })
            .OrderByDescending(x => x.Score)
            .Take(limit);

        var raw = await query.ToListAsync(ct);

        return raw.Select(x => new ScoredJob(x.JobId, x.Score, x.FullSim, x.SkillsSim, x.ExperienceSim)).ToList();
    }

    private static string F(double v) => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    private static string SW(double sim, double weight) => $"{sim:F2} @{weight * 100:F0}%";

    /// <summary>
    /// Rescales raw cosine similarity (typically 0.35–0.70 for text embeddings)
    /// to a more intuitive 0–100% range.
    /// </summary>
    internal static double NormalizeScore(double raw)
    {
        const double floor = 0.35; // below this = 0%
        const double ceiling = 0.62; // above this = 100%
        var normalized = (raw - floor) / (ceiling - floor);
        return Math.Clamp(normalized, 0, 1);
    }

}
