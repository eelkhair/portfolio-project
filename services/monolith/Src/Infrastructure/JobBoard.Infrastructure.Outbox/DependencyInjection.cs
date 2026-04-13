using JobBoard.Application.Interfaces.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.Outbox;

public static class DependencyInjection
{
    /// <summary>
    /// Registers only the outbox publisher (no background processor).
    /// Use this in secondary hosts (e.g. MCP server) that write outbox messages
    /// but should not compete with the main API for processing them.
    /// </summary>
    public static IServiceCollection AddOutboxPublisher(this IServiceCollection services)
    {
        services.AddTransient<IOutboxPublisher, OutboxPublisher>();
        return services;
    }

    /// <summary>
    /// Registers the outbox publisher AND the background processor.
    /// Only one host should call this to avoid duplicate event publishing.
    /// </summary>
    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services.AddOutboxPublisher();
        services.AddHostedService<OutboxProcessorBackgroundService>();
        return services;
    }
}
