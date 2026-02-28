using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Get;

public class CompanyJobSummaryDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int JobCount { get; set; }
    public List<JobSummaryItemDto> Jobs { get; set; } = [];
}

public class JobSummaryItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public JobType JobType { get; set; }
    public string? SalaryRange { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetCompanyJobSummariesQuery : BaseQuery<IQueryable<CompanyJobSummaryDto>>;

public class GetCompanyJobSummariesQueryHandler(IJobBoardQueryDbContext context, ILogger<GetCompanyJobSummariesQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetCompanyJobSummariesQuery, IQueryable<CompanyJobSummaryDto>>
{
    public Task<IQueryable<CompanyJobSummaryDto>> HandleAsync(GetCompanyJobSummariesQuery request, CancellationToken cancellationToken)
    {
        var result = Context.Companies.Select(c => new CompanyJobSummaryDto
        {
            CompanyId = c.Id,
            CompanyName = c.Name,
            JobCount = c.Jobs.Count,
            Jobs = c.Jobs.Select(j => new JobSummaryItemDto
            {
                Title = j.Title,
                Location = j.Location,
                JobType = j.JobType,
                SalaryRange = j.SalaryRange,
                CreatedAt = j.CreatedAt
            }).ToList()
        });

        return Task.FromResult(result);
    }
}
