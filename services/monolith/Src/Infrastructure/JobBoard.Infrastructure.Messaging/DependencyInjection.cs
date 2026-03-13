using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Messaging;

public static class DependencyInjection
{
    private const string DefaultRabbitMqHost = "amqp://guest:guest@192.168.1.160:5672/local";

    public static IServiceCollection AddMassTransitMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? DefaultRabbitMqHost;

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CloudEventsPublisher>>();
            return CloudEventsPublisher.CreateAsync(rabbitMqHost, logger).GetAwaiter().GetResult();
        });

        services.AddTransient<IOutboxMessageProcessor, MassTransitOutboxProcessor>();

        return services;
    }
}
