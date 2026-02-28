using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Get;

public class CompanyJobSummaryDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int JobCount { get; set; }
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
            JobCount = c.Jobs.Count
        });

        return Task.FromResult(result);
    }
}
