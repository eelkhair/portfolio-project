using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Infrastructure.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public class JobQueryService(IJobDbContext context): IJobQueryService
{
    public async Task<List<JobResponse>> ListAsync(Guid companyUId, CancellationToken ct)
    {
        var jobs = await context.Jobs.Where(c => c.Company.UId == companyUId)

            .Include(c=>c.Company)
            .Include(c=>c.Qualifications)
            .Include(c=>c.Responsibilities)
            .ToListAsync(ct);

        return jobs.Adapt<List<JobResponse>>();
    }

    public async Task<List<CompanyJobSummaryResponse>> ListCompanyJobSummariesAsync(CancellationToken ct)
    {
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

        return companies.Select(c => new CompanyJobSummaryResponse(
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
    }
}