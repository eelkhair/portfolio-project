using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class SettingsService(IRedisJsonStore store): ISettingsService
{
    private const string ProviderKey = "jobboard:config:ai-service-v2:AIProvider";
    private const string ModelKey = "jobboard:config:ai-service-v2:AIModel";

    public async Task<GetProviderResponse> GetProviderAsync()
    {
        var provider = await store.GetAsync<string>(ProviderKey);
        var model = await store.GetAsync<string>(ModelKey);

        var result = new GetProviderResponse
        {
            Provider = provider ?? "openai",
            Model = model ?? "gpt-4.1-mini"
        };

        Activity.Current?.SetTag("ai.provider", result.Provider);
        Activity.Current?.SetTag("ai.model", result.Model);

        return result;
    }

    public async Task<Unit> UpdateProviderAsync(UpdateProviderRequest request)
    {
        await store.SetAsync(ProviderKey, request.Provider);
        await store.SetAsync(ModelKey, request.Model);
        return Unit.Value;
    }
}