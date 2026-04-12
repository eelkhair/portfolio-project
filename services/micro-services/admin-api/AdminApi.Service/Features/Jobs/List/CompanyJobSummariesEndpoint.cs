using System.Diagnostics;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.List;

public class CompanyJobSummariesEndpoint(IJobQueryService service) : EndpointWithoutRequest<ApiResponse<List<CompanyJobSummaryResponse>>>
{
    public override void Configure()
    {
        Get("/companies/job-summaries");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "job");
        Activity.Current?.SetTag("operation", "list");
        var summaries = await service.ListCompanyJobSummariesAsync(ct);
        await Send.OkAsync(summaries, cancellation: ct);
    }
}
