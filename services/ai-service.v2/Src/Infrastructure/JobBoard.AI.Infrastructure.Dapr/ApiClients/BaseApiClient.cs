using Dapr.Client;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public abstract class BaseApiClient(DaprClient client, IUserAccessor accessor, ILogger logger)
{
    protected readonly DaprClient Client = client;

    protected HttpRequestMessage CreateRequest(HttpMethod method, string path, string serviceName)
    {
        var token = accessor.Token;
        var hasToken = !string.IsNullOrEmpty(token);
        var tokenPreview = hasToken ? token![..Math.Min(token.Length, 30)] + "..." : "(null/empty)";
        logger.LogInformation("Dapr invoke {Method} {Service}/{Path} — Token present: {HasToken}, Preview: {Preview}",
            method, serviceName, path, hasToken, tokenPreview);

        var request = Client.CreateInvokeMethodRequest(
            method,
            appId: serviceName,
            methodName: path);

        request.Headers.TryAddWithoutValidation("Authorization", token);
        return request;
    }
}