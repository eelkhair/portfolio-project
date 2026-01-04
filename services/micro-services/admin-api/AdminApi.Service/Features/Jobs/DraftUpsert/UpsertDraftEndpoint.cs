using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.DraftUpsert;

public sealed class UpsertDraftEndpoint(IJobCommandService service):
    Endpoint<JobDraftRequest, ApiResponse<JobDraftResponse>>
{
    private const string RouteTemplate = "jobs/{companyId}/save-draft";
    public override void Configure()
    {
        Put(RouteTemplate);
        Summary(s =>
        {
            s.Summary = "Create job draft";
            s.Description = "Creates or updates a job draft.";
        });
    }

    public override async Task HandleAsync(JobDraftRequest req, CancellationToken ct)
    {
        var companyId = Route<string>("companyId")!;
        var result = await service.CreateDraft(companyId, req, ct);
        await Send.OkAsync(result, ct);


    }
    
}

