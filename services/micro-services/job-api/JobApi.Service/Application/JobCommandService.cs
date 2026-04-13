using System.Security.Claims;
using Elkhair.Dev.Common.Dapr;
using JobApi.Application.Interfaces;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Models.Jobs.Responses;
using JobBoard.IntegrationEvents.Job;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Application;

public partial class JobCommandService(IJobDbContext context, IMessageSender messageSender, ILogger<JobCommandService> logger) : IJobCommandService
{
    public async Task<JobResponse> CreateJobAsync(CreateJobRequest request, ClaimsPrincipal user, CancellationToken ct, bool publishEvent = true)
    {
        LogCreatingJob(logger, request.Title, request.CompanyUId);

        var company = await context.Companies.FirstAsync(c => c.UId == request.CompanyUId, ct);
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

        LogJobCreated(logger, job.Title, job.UId);
        return job.Adapt<JobResponse>();
    }

    [LoggerMessage(LogLevel.Information, "Creating job '{JobTitle}' for company {CompanyUId}")]
    static partial void LogCreatingJob(ILogger logger, string jobTitle, Guid companyUId);

    [LoggerMessage(LogLevel.Information, "Job created: '{JobTitle}' ({JobUId})")]
    static partial void LogJobCreated(ILogger logger, string jobTitle, Guid jobUId);
}
