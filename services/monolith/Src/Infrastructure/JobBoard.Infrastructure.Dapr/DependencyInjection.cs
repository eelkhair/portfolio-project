using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.infrastructure.Dapr;

public static class DependencyInjection {
    public static IServiceCollection AddDaprServices(this IServiceCollection services)
    {
        services.AddDaprClient();
        services.AddTransient<IOutboxMessageProcessor, DaprOutboxMessageProcessor>();
        return services;
    }
}