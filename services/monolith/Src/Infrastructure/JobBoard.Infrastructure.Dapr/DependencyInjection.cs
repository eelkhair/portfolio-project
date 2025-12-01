using Dapr.Client;
using Dapr.Extensions.Configuration;
using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace JobBoard.infrastructure.Dapr;

public static class DependencyInjection {
    public static WebApplicationBuilder AddDaprServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDaprClient();
        builder.Configuration.AddDaprSecretStore("vault", 
            new DaprClientBuilder().Build(), 
            new Dictionary<string, string>
            {
                { "secret/data/portfolio/monolith", "Monolith" }
            });
       
        builder.Services.AddTransient<IOutboxMessageProcessor, DaprOutboxMessageProcessor>();
        return builder;
    }
}