using JobBoard.Domain;

// ReSharper disable UnusedMember.Global

namespace JobBoard.Application.Interfaces.Configurations;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(FeatureFlags feature);
    Task<IDictionary<string, bool>> GetAllFeaturesAsync();
}