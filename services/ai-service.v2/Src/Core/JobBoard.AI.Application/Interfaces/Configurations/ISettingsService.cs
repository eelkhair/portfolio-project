using JobBoard.AI.Application.Actions.Settings;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface ISettingsService
{
    Task<GetProviderResponse> GetProviderAsync();
    Task<Unit> UpdateProviderAsync(UpdateProviderRequest request);
}