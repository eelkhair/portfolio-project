using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetSimilarJobsQuery(Guid jobId) : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public Guid JobId { get; } = jobId;
}

public class GetSimilarJobsQueryHandler(IJobBoardQueryDbContext context, IAiServiceClient aiServiceClient, ILogger<GetSimilarJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetSimilarJobsQuery, List<JobResponse>>
{
    public async Task<List<JobResponse>> HandleAsync(GetSimilarJobsQuery request, CancellationToken cancellationToken)
    {
        var candidates = await aiServiceClient
            .GetSimilarJobs(request.JobId, cancellationToken);

        if (candidates.Count == 0)
            return [];

        var candidateIds = candidates.Select(c => c.JobId).ToList();

        var jobs = await Context.Jobs
            .Where(j => candidateIds.Contains(j.Id))
            .Include(j => j.Company)
            .Include(j => j.Responsibilities)
            .Include(j => j.Qualifications)
            .ToListAsync(cancellationToken);

        var jobMap = jobs.ToDictionary(j => j.Id);

        var orderedJobs = candidates
            .OrderBy(c => c.Rank)
            .Select(c => jobMap.GetValueOrDefault(c.JobId))
            .Where(j => j != null)
            .Take(3).ToList();

        return orderedJobs.Select(job => new JobResponse
        {
            Id = job!.Id,
            CompanyUId = job.Company.Id,
            Title = job.Title,
            JobType = job.JobType,
            Location = job.Location,
            CompanyName = job.Company.Name,
            AboutRole = job.AboutRole,
            SalaryRange = job.SalaryRange,
            Responsibilities = job.Responsibilities.Select(r => r.Value).ToList(),
            Qualifications = job.Qualifications.Select(q => q.Value).ToList()
        }).ToList();
    }
}

public sealed class SimilarJobCandidate
{
    public Guid JobId { get; set; }
    public double Similarity { get; set; }
    public int Rank { get; set; }
}
