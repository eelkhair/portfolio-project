using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Domain;
using Microsoft.FeatureManagement;

namespace JobBoard.Infrastructure.Configuration.Services;

public class FeatureFlagService(IFeatureManager featureManager) : IFeatureFlagService
{
    public Task<bool> IsEnabledAsync(FeatureFlags feature)
    {
        return featureManager.IsEnabledAsync(feature.ToString());
    }

    public async Task<IDictionary<string, bool>> GetAllFeaturesAsync()
    {
        var allFlags = new Dictionary<string, bool>();

        foreach (var flag in Enum.GetNames<FeatureFlags>())
        {
            allFlags[flag] = await featureManager.IsEnabledAsync(flag);
        }

        return allFlags;
    }
}