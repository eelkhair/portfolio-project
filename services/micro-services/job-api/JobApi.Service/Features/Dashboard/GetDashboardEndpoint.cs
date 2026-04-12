using System.Diagnostics;
using FastEndpoints;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Dashboard;

namespace JobApi.Features.Dashboard;

public class GetDashboardEndpoint(IDashboardQueryService service) : EndpointWithoutRequest<DashboardResponse>
{
    public override void Configure()
    {
        Get("/dashboard");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "dashboard");
        Activity.Current?.SetTag("operation", "get");
        var result = await service.GetDashboardAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
