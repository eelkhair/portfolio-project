using JobBoard.API.Mcp.Tools;
using JobBoard.Application;
using JobBoard.Infrastructure.BlobStorage;
using JobBoard.Infrastructure.Diagnostics;
using JobBoard.Infrastructure.HttpClients;
using JobBoard.Infrastructure.Keycloak;
using JobBoard.Infrastructure.Messaging;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.Smtp;
using JobBoard.Infrastructure.Turnstile;
using JobBoard.Mcp.Common;
using ModelContextProtocol.Protocol;

namespace JobBoard.API.Mcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMonolithMcpServices(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services
            .AddApplicationServices()
            .AddPersistenceServices(cfg)
            .AddOutboxPublisher()
            .AddMassTransitMessaging(cfg)
            .AddSmtpServices(cfg)
            .AddTurnstileVerifier()
            .AddBlobStorageServices(cfg)
            .AddAiServiceHttpClient(cfg)
            .AddHttpContextAccessor()
            .AddScoped<IUserAccessor, HttpUserAccessor>()
            .AddKeycloakAdminClient()
            .AddDiagnosticsServices(cfg, "monolith-mcp");

        services.AddKeycloakJwtAuth(cfg);
        services.AddAuthorization();

        services.AddScoped<HandlerDispatcher>();
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = "monolith-mcp",
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
