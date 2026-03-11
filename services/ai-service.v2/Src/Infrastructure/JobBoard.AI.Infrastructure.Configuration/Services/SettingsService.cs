using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Configuration;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class SettingsService(IRedisStore store, IConfiguration configuration): ISettingsService
{
    private const string ProviderKey = "jobboard:config:ai-service-v2:AIProvider";
    private const string ModelKey = "jobboard:config:ai-service-v2:AIModel";
    private const string IsMonolithKey = "jobboard:config:global:FeatureFlags:Monolith";
    private int ConfigDb { get; } = int.TryParse(configuration["Redis:ConfigDb"], out var db) ? db : 1;
    public async Task<GetProviderResponse> GetProviderAsync()
    {
        var provider = await store.GetAsync<string>(ProviderKey, ConfigDb);
        var model = await store.GetAsync<string>(ModelKey, ConfigDb);
        
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
        
        await store.SetAsync(ProviderKey, request.Provider, ConfigDb);
        await store.SetAsync(ModelKey, request.Model, ConfigDb);
        return Unit.Value;
    }

    public async Task<ApplicationModeDto> GetApplicationModeAsync()
    {
        var isMonolith = await store.GetAsync<string>(IsMonolithKey, ConfigDb);

        Activity.Current?.SetTag("isMonolith", isMonolith);
        return new ApplicationModeDto
        {
            IsMonolith = isMonolith == "true"
        };
    }
    
    public async Task<Unit> UpdateApplicationModeAsync(ApplicationModeDto request)
    {
        Activity.Current?.SetTag("isMonolith", request.IsMonolith);
        
        await store.SetAsync(IsMonolithKey, request.IsMonolith ? "true" : "false", ConfigDb);
        return Unit.Value;
    }
}