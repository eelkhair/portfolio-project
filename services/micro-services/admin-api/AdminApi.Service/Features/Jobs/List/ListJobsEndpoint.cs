using AdminApi.Application.Queries.Interfaces;
using JobAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.List;

public class ListJobsEndpoint(IJobQueryService service): Endpoint<ListJobsRequest,ApiResponse<List<JobResponse>>>
{
    public override void Configure()
    {
        Get("/jobs/{companyUId}");
        Permissions("read:jobs");
    }

    public override async Task HandleAsync(ListJobsRequest request, CancellationToken ct)
    {
        var jobs = await service.ListAsync(request.CompanyUId, ct);
        await Send.OkAsync( jobs , cancellation: ct);
    }
}

public record ListJobsRequest(Guid CompanyUId);