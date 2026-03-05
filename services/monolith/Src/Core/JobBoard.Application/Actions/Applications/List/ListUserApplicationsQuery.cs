using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Applications.List;

public class ListUserApplicationsQuery : BaseQuery<List<ApplicationResponse>>
{
}

public class ListUserApplicationsQueryHandler(IJobBoardQueryDbContext context, ILogger<ListUserApplicationsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListUserApplicationsQuery, List<ApplicationResponse>>
{
    public async Task<List<ApplicationResponse>> HandleAsync(ListUserApplicationsQuery query, CancellationToken cancellationToken)
    {
        var user = await Context.Users
            .FirstAsync(u => u.ExternalId == query.UserId, cancellationToken);

        return await Context.JobApplications
            .Where(a => a.UserId == user.InternalId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationResponse
            {
                Id = a.Id,
                JobId = a.Job.Id,
                JobTitle = a.Job.Title,
                CompanyName = a.Job.Company.Name,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
