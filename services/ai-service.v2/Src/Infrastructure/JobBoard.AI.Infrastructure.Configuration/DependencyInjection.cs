using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Configuration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace JobBoard.AI.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddSingleton<IRedisStore, RedisConfigurationStore>();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"] ?? "127.0.0.1:6379")
        );
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        return services;
    }
}
