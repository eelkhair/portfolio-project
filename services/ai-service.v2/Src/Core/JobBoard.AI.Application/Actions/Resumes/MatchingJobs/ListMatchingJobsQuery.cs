using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
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

        activity?.SetTag("matching.count", results.Count);
        return results;
    }

    private async Task<List<JobCandidate>> SimpleCosineSimilarity(Vector resumeVector, int limit, CancellationToken ct)
    {
        var similarities = await context.JobEmbeddings
            .OrderByDescending(e => 1 - e.VectorData.CosineDistance(resumeVector))
            .Select(c => new JobCandidate
            {
                JobId = c.JobId,
                Similarity = 1 - c.VectorData.CosineDistance(resumeVector),
            })
            .Take(limit)
            .ToListAsync(ct);

        for (var i = 0; i < similarities.Count; i++)
            similarities[i].Rank = i + 1;

        return similarities;
    }

    private async Task<List<JobCandidate>> WeightedCompositeSimilarity(
        ResumeEmbedding resume, int limit, CancellationToken ct)
    {
        var skillsW = resume.SkillsVectorData is not null ? WSkills : 0;
        var experienceW = resume.ExperienceVectorData is not null ? WExperience : 0;

        var usedSectionWeight = skillsW + experienceW;
        var fullWeight = WFull + (1.0 - WFull - usedSectionWeight);

        var total = fullWeight + usedSectionWeight;
        fullWeight /= total;
        skillsW /= total;
        experienceW /= total;

        // Load all job embeddings — acceptable for portfolio scale
        var jobEmbeddings = await context.JobEmbeddings.ToListAsync(ct);

        var scored = jobEmbeddings.Select(j =>
        {
            var fullSim = CosineSimilarity(resume.VectorData, j.VectorData);
            var skillsSim = resume.SkillsVectorData is not null ? CosineSimilarity(resume.SkillsVectorData, j.VectorData) : (double?)null;
            var experienceSim = resume.ExperienceVectorData is not null ? CosineSimilarity(resume.ExperienceVectorData, j.VectorData) : (double?)null;

            var score = fullWeight * fullSim
                + skillsW * (skillsSim ?? 0)
                + experienceW * (experienceSim ?? 0);

            return (JobId: j.JobId, Score: score, fullSim, skillsSim, experienceSim);
        })
        .OrderByDescending(x => x.Score)
        .Take(limit)
        .ToList();

        var results = new List<JobCandidate>(scored.Count);
        for (var i = 0; i < scored.Count; i++)
        {
            var s = scored[i];
            results.Add(new JobCandidate { JobId = s.JobId, Similarity = s.Score, Rank = i + 1 });

            var p = $"matching.result.{i + 1}";
            Activity.Current?.SetTag($"{p}.job_id", s.JobId);
            Activity.Current?.SetTag($"{p}.score", F(s.Score));
            Activity.Current?.SetTag($"{p}.full", SW(s.fullSim, fullWeight));
            if (s.skillsSim.HasValue) Activity.Current?.SetTag($"{p}.skills", SW(s.skillsSim.Value, skillsW));
            if (s.experienceSim.HasValue) Activity.Current?.SetTag($"{p}.experience", SW(s.experienceSim.Value, experienceW));
        }

        return results;
    }

    private static string F(double v) => v.ToString("F2");
    private static string SW(double sim, double weight) => $"{sim:F2} @{weight * 100:F0}%";

    private static double CosineSimilarity(Vector a, Vector b)
    {
        var aVals = a.ToArray();
        var bVals = b.ToArray();
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < aVals.Length; i++)
        {
            dot += aVals[i] * bVals[i];
            normA += aVals[i] * aVals[i];
            normB += bVals[i] * bVals[i];
        }
        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom == 0 ? 0 : dot / denom;
    }
}
