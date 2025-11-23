using System.Diagnostics.CodeAnalysis;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;

namespace Elkhair.Dev.Common.Dapr;
[ExcludeFromCodeCoverage]
public class MessageSender(ILogger<MessageSender> logger, DaprClient daprClient, UserContextService service) : IMessageSender
{
    public async Task SendEventAsync<T>(string pubSubName, string topic, string userId, T message, CancellationToken ct)
    { 
        var messageInfo = new {topic, userId, message};
        try
        {
            var idempotencyKey = service.GetHeader("Idempotency-Key");
            var eventModel = new EventDto<T>(userId, idempotencyKey?? Guid.NewGuid().ToString(), message);
            await daprClient.PublishEventAsync(pubSubName, topic, eventModel, cancellationToken: ct);
           
            logger.LogInformation("Event sent - {MessageInfo}", messageInfo);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending event - {MessageInfo}", messageInfo);
        }
    }

    public Task SendEventAsync<T>(string pubSubName, string topic, string userId, T message)
    {
        throw new NotImplementedException();
    }
}