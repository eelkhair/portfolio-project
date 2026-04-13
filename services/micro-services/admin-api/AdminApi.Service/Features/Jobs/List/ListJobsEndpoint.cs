using System.Diagnostics;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace AdminApi.Features.Jobs.List;

public class ListJobsEndpoint(IJobQueryService service) : Endpoint<ListJobsRequest, ApiResponse<List<JobResponse>>>
{
    public override void Configure()
    {
        Get("/jobs/{companyUId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListJobsRequest request, CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "job");
        Activity.Current?.SetTag("entity.id", request.CompanyUId);
        Activity.Current?.SetTag("operation", "list");
        var jobs = await service.ListAsync(request.CompanyUId, ct);
        await Send.OkAsync(jobs, cancellation: ct);
    }
}

public record ListJobsRequest(Guid CompanyUId);
