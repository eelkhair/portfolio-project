using System.Diagnostics;
using AdminAPI.Contracts.Models.Dashboard;
using AdminAPI.Contracts.Services;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Dashboard;

public class GetDashboardEndpoint(IDashboardQueryService service) : EndpointWithoutRequest<ApiResponse<DashboardResponse>>
{
    public override void Configure()
    {
        Get("/dashboard");
        Policies("Dashboard");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Activity.Current?.SetTag("entity.type", "dashboard");
        Activity.Current?.SetTag("operation", "get");
        var result = await service.GetDashboardAsync(ct);
        await Send.OkAsync(result, cancellation: ct);
    }
}
