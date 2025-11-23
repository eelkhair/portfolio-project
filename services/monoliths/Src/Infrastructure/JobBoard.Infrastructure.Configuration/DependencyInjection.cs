using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Infrastructure.Configuration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace JobBoard.Infrastructure.Configuration;

public static class AppConfigurationExtensions
{
    public static void AddSharedAzureAppConfiguration(this ConfigurationManager config)
    {
        config.AddAzureAppConfiguration(options =>
        {
            var connectionString = config.GetConnectionString("AppConfig") ??
               throw new InvalidOperationException(
                                       "The environment variable 'AppConfig' is not set or is empty.");
            options.ConfigureKeyVault(kv =>
            {
                kv.SetSecretResolver(_ => new ValueTask<string?>(default(string)));
            });
            ConfigureCommonOptions(options);
            options.Connect(connectionString);
        });
            
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true);
        return;

        void ConfigureCommonOptions(AzureAppConfigurationOptions options)
        {
            options.Select(KeyFilter.Any);
            options.UseFeatureFlags(featureFlagOptions => { featureFlagOptions.SetRefreshInterval(TimeSpan.FromMinutes(4)); });
            options.ConfigureRefresh(refreshOptions =>
            {
                refreshOptions.Register("Settings:Sentinel", refreshAll: true);
                refreshOptions.SetRefreshInterval(TimeSpan.FromMinutes(4));
            });
        }
    }
    
    public static IServiceCollection AddAppConfigurationServices(this IServiceCollection services)
    {
        services.AddScoped<IApplicationOrchestrator, ApplicationOrchestrator>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddAzureAppConfiguration();
        services.AddFeatureManagement();
        return services;
    }

    public static WebApplication AddAzureAppConfiguration(this WebApplication app)
    {
        app.UseAzureAppConfiguration();
        return app;
    }
}