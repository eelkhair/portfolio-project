using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Actions.Settings.Provider;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface ISettingsService
{
    Task<GetProviderResponse> GetProviderAsync();
    Task<Unit> UpdateProviderAsync(UpdateProviderRequest request);
    Task<ApplicationModeDto> GetApplicationModeAsync();
    
    Task<Unit> UpdateApplicationModeAsync(ApplicationModeDto request);
}