using System.Diagnostics;
using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Events;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using FastEndpoints;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Features.Jobs.Create;

public class CreateJobEndpoint(IJobCommandService service, 
    IMessageSender sender,
    ActivitySource activitySource) : Endpoint<JobCreateRequest, ApiResponse<JobResponse>>
{
    public override void Configure()
    {
        Post("/jobs");
    }

    public override async Task HandleAsync(JobCreateRequest req, CancellationToken ct)
    { 
        using var act = activitySource.StartActivity(
                     "job.create",
                     ActivityKind.Producer);

        act?.SetTag("job.create.start.sql", req);
        var response = await service.CreateJob(req, ct);
        act?.SetTag("job.create.end.sql", response);
        if (response.Success && response.Data is { } job)
        {
            var publishedEvent = new JobPublishedEvent
            {
                UId = job.UId,
                Title = job.Title,
                CompanyUId = job.CompanyUId,
                CompanyName = job.CompanyName,
                Location = job.Location,
                JobType = job.JobType.ToString(),
                AboutRole = job.AboutRole,
                SalaryRange = job.SalaryRange,
                Responsibilities = job.Responsibilities,
                Qualifications = job.Qualifications,
                CreatedAt = job.CreatedAt,
                UpdatedAt = job.UpdatedAt,
                DraftId = req.DraftId,
                DeleteDraft = req.DeleteDraft
            };

            await sender.SendEventAsync(PubSubNames.RabbitMq, "job.published.v2", User.GetUserId(), publishedEvent, ct);
        }
       
    
        await Send.OkAsync(response, ct);
    }
}