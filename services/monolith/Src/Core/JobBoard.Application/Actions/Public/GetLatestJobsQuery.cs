using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetLatestJobsQuery(int count = 6) : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public int Count { get; } = count;
}

public class GetLatestJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<GetLatestJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetLatestJobsQuery, List<JobResponse>>
{
    public async Task<List<JobResponse>> HandleAsync(GetLatestJobsQuery request, CancellationToken cancellationToken)
    {
        return await Context.Jobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(request.Count)
            .Select(job => new JobResponse
            {
                Id = job.Id,
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
            })
            .ToListAsync(cancellationToken);
    }
}
