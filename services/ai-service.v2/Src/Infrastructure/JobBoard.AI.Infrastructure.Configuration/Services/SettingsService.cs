using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class SettingsService(IRedisStore store): ISettingsService
{
    private const string ProviderKey = "jobboard:config:ai-service-v2:AIProvider";
    private const string ModelKey = "jobboard:config:ai-service-v2:AIModel";
    private const string IsMonolithKey = "jobboard:config:global:FeatureFlags:Monolith";
    public async Task<GetProviderResponse> GetProviderAsync()
    {
        var provider = await store.GetAsync<string>(ProviderKey, 1);
        var model = await store.GetAsync<string>(ModelKey,1 );
        
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
        Activity.Current?.SetTag("ai.provider", request.Provider);
        Activity.Current?.SetTag("ai.model", request.Model);
        
        await store.SetAsync(ProviderKey, request.Provider, 1);
        await store.SetAsync(ModelKey, request.Model, 1);
        return Unit.Value;
    }

    public async Task<ApplicationModeDto> GetApplicationModeAsync()
    {
        var isMonolith = await store.GetAsync<string>(IsMonolithKey, 1);

        Activity.Current?.SetTag("isMonolith", isMonolith);
        return new ApplicationModeDto
        {
            IsMonolith = isMonolith == "true"
        };
    }
    
    public async Task<Unit> UpdateApplicationModeAsync(ApplicationModeDto request)
    {
        Activity.Current?.SetTag("isMonolith", request.IsMonolith);
        
        await store.SetAsync(IsMonolithKey, request.IsMonolith ? "true" : "false", 1);
        return Unit.Value;
    }
}