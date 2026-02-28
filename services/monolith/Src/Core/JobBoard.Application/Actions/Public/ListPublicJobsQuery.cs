using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class ListPublicJobsQuery : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public string? Search { get; set; }
    public JobType? JobType { get; set; }
    public string? Location { get; set; }
}

public class ListPublicJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<ListPublicJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListPublicJobsQuery, List<JobResponse>>
{
    public async Task<List<JobResponse>> HandleAsync(ListPublicJobsQuery request, CancellationToken cancellationToken)
    {
        return await Context.Jobs
            .OrderByDescending(j => j.CreatedAt)
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
