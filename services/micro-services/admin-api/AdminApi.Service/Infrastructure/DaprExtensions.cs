using Dapr.Client;
using Dapr.Extensions.Configuration;

namespace AdminApi.Infrastructure;

public static class DaprExtensions
{
    public static WebApplicationBuilder AddDaprServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDaprClient();
        builder.Configuration.AddDaprSecretStore("vault", 
            new DaprClientBuilder().Build(), 
            new Dictionary<string, string>());
       
        return builder;
    }
}