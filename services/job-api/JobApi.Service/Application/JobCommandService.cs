using System.Security.Claims;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public class JobCommandService(IJobDbContext context) : IJobCommandService
{
    public async Task<JobResponse> CreateJobAsync(CreateJobRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        var company = await context.Companies.FirstAsync(c=> c.UId == request.CompanyUId, ct);
        var job = request.Adapt<Job>();
        job.CompanyId = company.Id;


        context.Jobs.Add(job);
        await context.SaveChangesAsync(user, ct);
        return job.Adapt<JobResponse>();
    }
}