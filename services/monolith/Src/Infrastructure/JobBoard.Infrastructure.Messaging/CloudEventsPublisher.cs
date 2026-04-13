using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace JobBoard.Infrastructure.Messaging;

public sealed class CloudEventsPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<CloudEventsPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private CloudEventsPublisher(IConnection connection, IChannel channel, ILogger<CloudEventsPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<CloudEventsPublisher> CreateAsync(string rabbitMqHost, ILogger<CloudEventsPublisher> logger)
    {
        var factory = new ConnectionFactory { Uri = new Uri(rabbitMqHost) };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        return new CloudEventsPublisher(connection, channel, logger);
    }

    public async Task PublishAsync(string exchange, object data, string eventType, string source, string? traceparent, CancellationToken ct)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Fanout,
            durable: false,
            autoDelete: false,
            cancellationToken: ct);

        var cloudEvent = new Dictionary<string, object>
(StringComparer.Ordinal)
        {
            ["specversion"] = "1.0",
            ["type"] = "com.dapr.event.sent",
            ["source"] = source,
            ["id"] = Guid.CreateVersion7().ToString(),
            ["time"] = DateTime.UtcNow.ToString("O"),
            ["datacontenttype"] = "application/json",
            ["pubsubname"] = "rabbitmq.pubsub",
            ["topic"] = exchange,
            ["traceid"] = traceparent ?? "",
            ["traceparent"] = traceparent ?? "",
            ["tracestate"] = "",
            ["data"] = data
        };

        var json = JsonSerializer.Serialize(cloudEvent, JsonOpts);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/cloudevents+json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: "",
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation("Published CloudEvent to exchange '{Exchange}' type '{EventType}'", exchange, eventType);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
