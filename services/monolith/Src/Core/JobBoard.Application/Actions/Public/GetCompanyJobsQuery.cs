using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetCompanyJobsQuery(Guid companyId) : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public Guid CompanyId { get; } = companyId;
}

public class GetCompanyJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<GetCompanyJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetCompanyJobsQuery, List<JobResponse>>
{
    public async Task<List<JobResponse>> HandleAsync(GetCompanyJobsQuery request, CancellationToken cancellationToken)
    {
        return await Context.Jobs
            .Where(j => j.Company.Id == request.CompanyId)
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
                Responsibilities = job.Responsibilities.Select(c => c.Value).ToList(),
                Qualifications = job.Qualifications.Select(c => c.Value).ToList(),
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
