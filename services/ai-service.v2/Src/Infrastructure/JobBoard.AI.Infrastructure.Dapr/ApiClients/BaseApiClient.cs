using Dapr.Client;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public abstract class BaseApiClient(DaprClient client, IUserAccessor accessor)
{
    protected readonly DaprClient Client = client;

    protected HttpRequestMessage CreateRequest(HttpMethod method, string path, string serviceName)
    {
        var request = Client.CreateInvokeMethodRequest(
            method,
            appId: serviceName,
            methodName: path);

        request.Headers.TryAddWithoutValidation("Authorization", accessor.Token);
        return request;
    }
}