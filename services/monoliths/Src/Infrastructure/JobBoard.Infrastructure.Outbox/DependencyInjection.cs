using Confluent.Kafka;
using JobBoard.Application.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.Outbox;

public static class DependencyInjection
{
    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
            
            return services;
    }
    public static IServiceCollection AddKafkaServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var producerConfig = new ProducerConfig();
        configuration.GetSection("Kafka").Bind(producerConfig);

        services.AddSingleton(producerConfig);
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = sp.GetRequiredService<ProducerConfig>();
            return new ProducerBuilder<string, string>(config).Build();
        });
        return services;
    }

}