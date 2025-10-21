using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Requests;
using Elkhair.Dev.Common.Application;
using FastEndpoints;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Features.Jobs.Create;

public class CreateJobEndpoint(IJobCommandService service) : Endpoint<JobCreateRequest, ApiResponse<JobResponse>>
{
    public override void Configure()
    {
        Post("/jobs");
    }

    public override async Task HandleAsync(JobCreateRequest req, CancellationToken ct)
    {
        var response = await service.CreateJob(req, ct);
        if (response.Success)
        {
            if (req.DeleteDraft)
            {
                var deleteResponse = await service.DeleteDraft(req.DraftId, req.CompanyUId.ToString(), ct);
                if (!deleteResponse.Success)
                {
                    response.StatusCode= deleteResponse.StatusCode;
                    response.Exceptions= deleteResponse.Exceptions;
                    response.Success= false;
                }
            }
        }
    
        await SendOkAsync(response, ct);
    }
}