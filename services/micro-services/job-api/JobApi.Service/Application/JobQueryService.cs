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
        return await context.Companies
            .Select(c => new CompanyJobSummaryResponse(
                c.UId,
                c.Name,
                c.Jobs.Count))
            .ToListAsync(ct);
    }
}