using System.Diagnostics;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.DraftUpsert;

public sealed class UpsertDraftEndpoint(IJobCommandService service) :
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
        Activity.Current?.SetTag("entity.type", "draft");
        Activity.Current?.SetTag("entity.id", companyId);
        Activity.Current?.SetTag("operation", "create");
        var result = await service.CreateDraft(companyId, req, ct);
        await Send.OkAsync(result, ct);


    }

}

