using FastEndpoints;
using JobApi.Infrastructure.Data;
using JobAPI.Contracts.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Features.Dashboard;

public class GetDashboardEndpoint(IJobDbContext db) : EndpointWithoutRequest<DashboardResponse>
{
    public override void Configure()
    {
        Get("/dashboard");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var jobCount = await db.Jobs.CountAsync(ct);
        var companyCount = await db.Companies.CountAsync(ct);
        var draftCount = await db.Drafts.CountAsync(ct);

        var jobsByType = await db.Jobs
            .GroupBy(j => j.JobType)
            .Select(g => new LabelCountItem
            {
                Label = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync(ct);

        var jobsByLocation = await db.Jobs
            .GroupBy(j => j.Location)
            .Select(g => new LabelCountItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(ct);

        var topCompanies = await db.Jobs
            .Include(j => j.Company)
            .GroupBy(j => j.Company.Name)
            .Select(g => new LabelCountItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToListAsync(ct);

        var recentJobs = await db.Jobs
            .Include(j => j.Company)
            .OrderByDescending(j => j.CreatedAt)
            .Take(5)
            .Select(j => new RecentJobItem
            {
                Title = j.Title,
                CompanyName = j.Company.Name,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync(ct);

        await Send.OkAsync(new DashboardResponse
        {
            JobCount = jobCount,
            CompanyCount = companyCount,
            DraftCount = draftCount,
            JobsByType = jobsByType,
            JobsByLocation = jobsByLocation,
            TopCompanies = topCompanies,
            RecentJobs = recentJobs
        }, ct);
    }
}
