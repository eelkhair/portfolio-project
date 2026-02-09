using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Configuration.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace JobBoard.AI.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddSingleton<IRedisJsonStore, RedisJsonStore>();
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect("192.168.1.160:6379")
        );
        return services;
    }
}
