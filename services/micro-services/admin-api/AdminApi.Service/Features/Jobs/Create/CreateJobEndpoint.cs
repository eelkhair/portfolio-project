using System.Diagnostics;
using AdminApi.Application.Commands.Interfaces;
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
        if (response.Success)
        {
            if (req.DeleteDraft)
            {
                using var draftTrace = act?.SetTag("job.delete.draft.start", req.DraftId);
                var deleteResponse = await service.DeleteDraft(req.DraftId, req.CompanyUId.ToString(), ct);
                if (!deleteResponse.Success)
                {
                    response.StatusCode= deleteResponse.StatusCode;
                    response.Exceptions= deleteResponse.Exceptions;
                    response.Success= false;
                }
                draftTrace?.SetTag("job.delete.draft.end", deleteResponse);
            }
            
            await sender.SendEventAsync(PubSubNames.RabbitMq, "job.published", User.GetUserId(), response.Data, ct);
        }
       
    
        await Send.OkAsync(response, ct);
    }
}