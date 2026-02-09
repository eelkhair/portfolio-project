using JobBoard.AI.Application.Actions.Settings;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface ISettingsService
{
    Task<Unit> UpdateProviderAsync(UpdateProviderRequest request);
}