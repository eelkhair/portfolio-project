using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobApi.Application;

public partial class JobQueryService(IJobDbContext context, ILogger<JobQueryService> logger): IJobQueryService
{
    public async Task<List<JobResponse>> ListAsync(Guid companyUId, CancellationToken ct)
    {
        LogListingJobs(logger, companyUId);

        var jobs = await context.Jobs.Where(c => c.Company.UId == companyUId)

            .Include(c=>c.Company)
            .Include(c=>c.Qualifications)
            .Include(c=>c.Responsibilities)
            .ToListAsync(ct);

        LogJobsListed(logger, companyUId, jobs.Count);
        return jobs.Adapt<List<JobResponse>>();
    }

    public async Task<List<CompanyJobSummaryResponse>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
        LogListingCompanyJobSummaries(logger);

        var companies = await context.Companies
            .Select(c => new
            {
                c.UId,
                c.Name,
                Jobs = c.Jobs.Select(j => new
                {
                    j.Title,
                    j.Location,
                    j.JobType,
                    j.SalaryRange,
                    j.CreatedAt
                }).ToList()
            })
            .ToListAsync(ct);

        var result = companies.Select(c => new CompanyJobSummaryResponse(
            c.UId,
            c.Name,
            c.Jobs.Count,
            c.Jobs.Select(j => new JobSummaryItem(
                j.Title,
                j.Location,
                j.JobType.ToString(),
                j.SalaryRange,
                j.CreatedAt
            )).ToList()
        )).ToList();

        LogCompanyJobSummariesListed(logger, result.Count);
        return result;
    }

    [LoggerMessage(LogLevel.Information, "Listing jobs for company {CompanyUId}")]
    static partial void LogListingJobs(ILogger logger, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Listed {Count} jobs for company {CompanyUId}")]
    static partial void LogJobsListed(ILogger logger, Guid companyUId, int count);

    [LoggerMessage(LogLevel.Information, "Listing company job summaries")]
    static partial void LogListingCompanyJobSummaries(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Listed {Count} company job summaries")]
    static partial void LogCompanyJobSummariesListed(ILogger logger, int count);
}