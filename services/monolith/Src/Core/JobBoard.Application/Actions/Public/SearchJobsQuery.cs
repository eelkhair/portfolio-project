using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class SearchJobsQuery : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public string? Query { get; set; }
    public int Limit { get; set; } = 30;
    public string? Location { get; set; }
    public string? JobType { get; set; }
}

public class SearchJobsQueryHandler(
    IJobBoardQueryDbContext context,
    IAiServiceClient aiServiceClient,
    ILogger<SearchJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<SearchJobsQuery, List<JobResponse>>
{
    public async Task<List<JobResponse>> HandleAsync(SearchJobsQuery request, CancellationToken cancellationToken)
    {
        var candidates = await aiServiceClient.SearchJobs(
            request.Query, request.Location, request.JobType, request.Limit, cancellationToken);

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

        var rankedJobs = candidates
            .OrderBy(c => c.Rank)
            .Select(c => jobMap.GetValueOrDefault(c.JobId))
            .Where(j => j != null)
            .ToList();

        var orderedJobs = (!string.IsNullOrEmpty(request.JobType) && Enum.TryParse<JobType>(request.JobType, true, out var jobType)
            ? rankedJobs.OrderBy(j => j!.JobType == jobType ? 0 : 1).ToList()
            : rankedJobs)
            .Take(request.Limit)
            .ToList();
        
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
            Qualifications = job.Qualifications.Select(q => q.Value).ToList(),
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
            
        }).ToList();
    }
}