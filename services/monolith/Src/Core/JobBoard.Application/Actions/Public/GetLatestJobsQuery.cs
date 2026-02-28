using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetLatestJobsQuery(int count = 6) : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public int Count { get; } = count;
}

public class GetLatestJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<GetLatestJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetLatestJobsQuery, List<JobResponse>>
{
    public Task<List<JobResponse>> HandleAsync(GetLatestJobsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
