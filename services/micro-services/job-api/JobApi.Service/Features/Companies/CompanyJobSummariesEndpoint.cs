using FastEndpoints;
using JobApi.Application.Interfaces;

namespace JobApi.Features.Companies;

public class CompanyJobSummariesEndpoint(IJobQueryService service) : EndpointWithoutRequest<List<CompanyJobSummaryResponse>>
{
    public override void Configure()
    {
        Get("/companies/job-summaries");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var summaries = await service.ListCompanyJobSummariesAsync(ct);
        await Send.OkAsync(summaries, cancellation: ct);
    }
}
