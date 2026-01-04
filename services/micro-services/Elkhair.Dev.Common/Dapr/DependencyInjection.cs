using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elkhair.Dev.Common.Dapr;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Method intentionally left empty.
    }
     
    public static void AddMessageSender(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddTransient<UserContextService>();
        services.AddTransient<IMessageSender, MessageSender>( sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MessageSender>>();
            var accessor = sp.GetRequiredService<UserContextService>();
            var client = new DaprClientBuilder().Build();
            
            return new MessageSender(logger, client, accessor);
        });
    }
    
    public static void AddStateManager(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddTransient<IStateManager, StateManager>( sp =>
        {
            var logger = sp.GetRequiredService<ILogger<StateManager>>();
            var client = new DaprClientBuilder().Build();
            
            return new StateManager(logger, client);
        });
    }
}