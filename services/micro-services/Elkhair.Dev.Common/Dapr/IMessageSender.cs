namespace Elkhair.Dev.Common.Dapr;

public interface IMessageSender
{
    Task SendEventAsync<T>(string pubSubName, string topic, string userId, T message, CancellationToken ct);
}