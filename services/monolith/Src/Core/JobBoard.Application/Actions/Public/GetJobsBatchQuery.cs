using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetJobsBatchQuery(List<Guid> jobIds) : BaseQuery<List<JobBatchResponse>>, IAnonymousRequest
{
    public List<Guid> JobIds { get; } = jobIds;
}

public class GetJobsBatchQueryHandler(
    IJobBoardQueryDbContext context,
    ILogger<GetJobsBatchQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetJobsBatchQuery, List<JobBatchResponse>>
{
    public async Task<List<JobBatchResponse>> HandleAsync(GetJobsBatchQuery request, CancellationToken cancellationToken)
    {
        if (request.JobIds.Count == 0)
            return [];

        var jobs = await Context.Jobs
            .Where(j => request.JobIds.Contains(j.Id))
            .Include(j => j.Responsibilities)
            .Include(j => j.Qualifications)
            .Select(j => new JobBatchResponse
            {
                JobId = j.Id,
                Title = j.Title,
                AboutRole = j.AboutRole,
                Location = j.Location,
                JobType = j.JobType.ToString(),
                SalaryRange = j.SalaryRange,
                Responsibilities = j.Responsibilities.Select(r => r.Value).ToList(),
                Qualifications = j.Qualifications.Select(q => q.Value).ToList()
            })
            .ToListAsync(cancellationToken);

        return jobs;
    }
}
