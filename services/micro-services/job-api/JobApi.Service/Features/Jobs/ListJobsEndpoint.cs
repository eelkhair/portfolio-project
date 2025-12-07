using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobApi.Features.Jobs;

public class ListJobsEndpoint(IJobQueryService service): Endpoint<ListJobsRequest,List<JobResponse>>
{
    public override void Configure()
    {
        Get("/jobs/{companyUId}");
        Permissions("read:jobs");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListJobsRequest request, CancellationToken ct)
    {
        var jobs = await service.ListAsync(request.CompanyUId, ct);
        await Send.OkAsync( jobs , cancellation: ct);
    }
}

public record ListJobsRequest(Guid CompanyUId);