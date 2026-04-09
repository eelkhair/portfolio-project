using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Dashboard;

public class GetDashboardQuery : BaseQuery<DashboardResponse>;

public class GetDashboardQueryHandler(IJobBoardQueryDbContext context, ILogger<GetDashboardQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<DashboardResponse> HandleAsync(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var jobCount = await Context.Jobs.CountAsync(cancellationToken);
        var companyCount = await Context.Companies.CountAsync(cancellationToken);
        var draftCount = await Context.Drafts.CountAsync(cancellationToken);

        var jobsByType = await Context.Jobs
            .GroupBy(j => j.JobType)
            .Select(g => new LabelCountItem
            {
                Label = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var jobsByLocation = await Context.Jobs
            .GroupBy(j => j.Location)
            .Select(g => new LabelCountItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);

        var topCompanies = await Context.Jobs
            .Include(j => j.Company)
            .GroupBy(j => j.Company.Name)
            .Select(g => new LabelCountItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(cancellationToken);

        var recentJobs = await Context.Jobs
            .Include(j => j.Company)
            .OrderByDescending(j => j.CreatedAt)
            .Take(5)
            .Select(j => new RecentJobItem
            {
                Title = j.Title,
                CompanyName = j.Company.Name,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new DashboardResponse
        {
            JobCount = jobCount,
            CompanyCount = companyCount,
            DraftCount = draftCount,
            JobsByType = jobsByType,
            JobsByLocation = jobsByLocation,
            TopCompanies = topCompanies,
            RecentJobs = recentJobs
        };
    }
}
