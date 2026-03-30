using AdminApi.Core;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using JobBoard.Mcp.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using AdminApi.Mcp.Tools;

namespace AdminApi.Mcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAdminMcpServices(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddOpenTelemetryServices(cfg, "admin-api-mcp");
        services.AddHttpContextAccessor();
        services.AddMessageSender();
        services.AddStateManager();
        services.AddAdminApiCoreServices();

        services.AddKeycloakJwtAuth(cfg);
        services.AddAuthorization();

        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "admin-api-mcp",
                    Version = "1.0.0"
                };
            })
            .WithTools<CompanyTools>()
            .WithTools<JobTools>()
            .WithTools<DraftTools>()
            .WithTools<IndustryTools>()
            .WithHttpTransport(transport => { transport.Stateless = true; });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        services.AddHealthChecks();

        return services;
    }
}
