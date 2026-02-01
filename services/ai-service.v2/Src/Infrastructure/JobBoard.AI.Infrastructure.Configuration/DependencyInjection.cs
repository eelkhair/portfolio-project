using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Infrastructure.Configuration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();

        return services;
    }
}
