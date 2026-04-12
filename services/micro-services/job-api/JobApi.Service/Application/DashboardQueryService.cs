using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobAPI.Contracts.Models.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobApi.Application;

public partial class DashboardQueryService(IJobDbContext db, ILogger<DashboardQueryService> logger) : IDashboardQueryService
{
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct)
    {
        LogFetchingDashboard(logger);

        var jobCount = await db.Jobs.CountAsync(ct);
        var companyCount = await db.Companies.CountAsync(ct);
        var draftCount = await db.Drafts.CountAsync(ct);

        LogDashboardCounts(logger, jobCount, companyCount, draftCount);

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

    [LoggerMessage(LogLevel.Information, "Fetching dashboard data")]
    static partial void LogFetchingDashboard(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Dashboard loaded: {JobCount} jobs, {CompanyCount} companies, {DraftCount} drafts")]
    static partial void LogDashboardCounts(ILogger logger, int jobCount, int companyCount, int draftCount);
}
