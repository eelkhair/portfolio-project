using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetCompanyJobsQuery(Guid companyId) : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public Guid CompanyId { get; } = companyId;
}

public class GetCompanyJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<GetCompanyJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetCompanyJobsQuery, List<JobResponse>>
{
    public Task<List<JobResponse>> HandleAsync(GetCompanyJobsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
