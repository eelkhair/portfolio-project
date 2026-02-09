using AdminAPI.Contracts.Models.Settings;
using Elkhair.Dev.Common.Application;

namespace AdminApi.Application.Commands.Interfaces;

public interface ISettingsCommandService
{
    Task<ApiResponse<GetProviderResponse>> GetProviderAsync(CancellationToken ct = default);
    Task<ApiResponse<UpdateProviderResponse>> UpdateProviderAsync(UpdateProviderRequest request, CancellationToken ct = default);
}
