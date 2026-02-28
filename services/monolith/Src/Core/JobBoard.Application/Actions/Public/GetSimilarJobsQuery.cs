using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetSimilarJobsQuery(Guid jobId, Guid companyUId, JobType jobType) : BaseQuery<List<JobResponse>>, IAnonymousRequest
{
    public Guid JobId { get; } = jobId;
    public Guid CompanyUId { get; } = companyUId;
    public JobType JobType { get; } = jobType;
}

public class GetSimilarJobsQueryHandler(IJobBoardQueryDbContext context, ILogger<GetSimilarJobsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetSimilarJobsQuery, List<JobResponse>>
{
    public Task<List<JobResponse>> HandleAsync(GetSimilarJobsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
