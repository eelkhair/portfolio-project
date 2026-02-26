using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Jobs.List;

public class ListJobsQuery(Guid companyUId) : BaseQuery<IQueryable<JobResponse>>
{
    public Guid CompanyUId { get; } = companyUId;
}

public class ListJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<ListJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListJobsQuery, IQueryable<JobResponse>>
{
    public Task<IQueryable<JobResponse>> HandleAsync(ListJobsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching jobs from the database...");
        
        var result = Context.Jobs.Where(c=>c.Company.Id == request.CompanyUId).Select(job => new JobResponse
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
            
        });
        
        return Task.FromResult(result);
    }
}