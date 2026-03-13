using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.Infrastructure.HttpClients;

public static class DependencyInjection
{
    public static IServiceCollection AddAiServiceHttpClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<AuthorizationForwardingHandler>();

        services.AddHttpClient<IAiServiceClient, HttpAiServiceClient>(client =>
            {
                var baseUrl = configuration["AiServiceUrl"]
                              ?? "http://localhost:5200/";
                client.BaseAddress = new Uri(baseUrl);
            })
            .AddHttpMessageHandler<AuthorizationForwardingHandler>()
            .AddStandardResilienceHandler();

        return services;
    }
}
