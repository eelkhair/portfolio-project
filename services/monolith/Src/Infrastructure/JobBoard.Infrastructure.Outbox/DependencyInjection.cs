using JobBoard.Application.Interfaces.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.Outbox;

public static class DependencyInjection
{
    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services.AddTransient<IOutboxPublisher, OutboxPublisher>();
            
            return services;
    }
}