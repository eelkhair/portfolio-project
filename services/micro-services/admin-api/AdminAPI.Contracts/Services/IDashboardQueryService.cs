using AdminAPI.Contracts.Models.Dashboard;
using Elkhair.Dev.Common.Application;

namespace AdminAPI.Contracts.Services;

public interface IDashboardQueryService
{
    Task<ApiResponse<DashboardResponse>> GetDashboardAsync(CancellationToken ct);
}
