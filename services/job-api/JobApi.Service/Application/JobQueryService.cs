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
        var jobs = await context.Jobs.Where(c => c.Company.UId == companyUId).ToListAsync(ct);
        
        return jobs.Adapt<List<JobResponse>>();
    }
}