using JobAPI.Contracts.Models.Dashboard;

namespace JobApi.Application.Interfaces;

public interface IDashboardQueryService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct);
}
