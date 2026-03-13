using JobBoard.AI.Application.Interfaces.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.AI.Infrastructure.HttpClients;

public static class DependencyInjection
{
    public static IServiceCollection AddMonolithHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuthorizationForwardingHandler>();

        services.AddHttpClient<IMonolithApiClient, HttpMonolithApiClient>(client =>
            {
                var baseUrl = configuration["Services:MonolithApi:BaseUrl"]
                              ?? "http://localhost:5280/";
                client.BaseAddress = new Uri(baseUrl);
            })
            .AddHttpMessageHandler<AuthorizationForwardingHandler>()
            .AddStandardResilienceHandler();

        return services;
    }
}
