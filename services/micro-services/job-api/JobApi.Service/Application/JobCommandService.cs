using System.Security.Claims;
using Elkhair.Dev.Common.Dapr;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobBoard.IntegrationEvents.Job;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public class JobCommandService(IJobDbContext context, IMessageSender messageSender) : IJobCommandService
{
    public async Task<JobResponse> CreateJobAsync(CreateJobRequest request, ClaimsPrincipal user, CancellationToken ct, bool publishEvent = true)
    {
        var company = await context.Companies.FirstAsync(c=> c.UId == request.CompanyUId, ct);
        var job = request.Adapt<Job>();
        job.CompanyId = company.Id;


        context.Jobs.Add(job);
        await context.SaveChangesAsync(user, ct);

        if (publishEvent)
        {
            var userId = user.FindFirst("sub")?.Value ?? "system";
            var evt = new MicroJobCreatedV1Event(
                job.UId, request.CompanyUId, job.Title, job.AboutRole,
                job.Location, job.SalaryRange, job.JobType.ToString(),
                job.Responsibilities.Select(r => r.Value).ToList(),
                job.Qualifications.Select(q => q.Value).ToList())
            {
                UserId = userId
            };
            await messageSender.SendEventAsync("rabbitmq.pubsub", "micro.job-created.v1",
                userId, evt, ct);
        }

        return job.Adapt<JobResponse>();
    }
}