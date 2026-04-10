using System.Diagnostics;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Actions.Settings.FeatureFlags;
using JobBoard.AI.Application.Actions.Settings.Provider;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace JobBoard.AI.Infrastructure.Configuration.Services;

public class SettingsService(IRedisStore store, IConfiguration configuration, IConnectionMultiplexer mux): ISettingsService
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

    private const string FeatureFlagPrefix = "jobboard:config:global:FeatureFlags:";

    public async Task<List<UpdateFeatureFlagRequest>> GetFeatureFlagsAsync()
    {
        var db = mux.GetDatabase(ConfigDb);
        var server = mux.GetServers().First();
        var flags = new List<UpdateFeatureFlagRequest>();

        await foreach (var key in server.KeysAsync(database: ConfigDb, pattern: $"{FeatureFlagPrefix}*"))
        {
            var value = await db.StringGetAsync(key);
            var name = key.ToString().Replace(FeatureFlagPrefix, "");
            flags.Add(new UpdateFeatureFlagRequest
            {
                Name = name,
                Enabled = value.HasValue && value.ToString() == "true"
            });
        }

        return flags.OrderBy(f => f.Name).ToList();
    }

    public async Task<Unit> UpdateFeatureFlagAsync(UpdateFeatureFlagRequest request)
    {
        Activity.Current?.SetTag("featureFlag.name", request.Name);
        Activity.Current?.SetTag("featureFlag.enabled", request.Enabled);

        await store.SetAsync($"{FeatureFlagPrefix}{request.Name}", request.Enabled ? "true" : "false", ConfigDb);
        return Unit.Value;
    }
}