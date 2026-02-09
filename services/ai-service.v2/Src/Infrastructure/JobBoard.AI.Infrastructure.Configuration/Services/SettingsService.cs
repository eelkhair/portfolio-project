using JobBoard.AI.Application.Actions.Settings;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class SettingsService(IRedisJsonStore store): ISettingsService
{
    public async Task<Unit> UpdateProviderAsync(UpdateProviderRequest request)
    {
        await store.SetAsync("jobboard:config:ai-service-v2:AIProvider", request.Provider);
        await store.SetAsync("jobboard:config:ai-service-v2:AIModel", request.Model);
        return Unit.Value;
    }
}