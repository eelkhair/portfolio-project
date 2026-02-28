using AdminApi.Application.Queries.Interfaces;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.List;

public class CompanyJobSummariesEndpoint(IJobQueryService service) : EndpointWithoutRequest<ApiResponse<List<CompanyJobSummaryResponse>>>
{
    public override void Configure()
    {
        Get("/companies/job-summaries");
        Permissions("read:jobs");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var summaries = await service.ListCompanyJobSummariesAsync(ct);
        await Send.OkAsync(summaries, cancellation: ct);
    }
}
