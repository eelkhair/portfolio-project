using JobBoard.API.Mcp.Tools;
using JobBoard.Application;
using JobBoard.Application.Interfaces;
using JobBoard.Infrastructure.BlobStorage;
using JobBoard.Infrastructure.Configuration.Services;
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
            // ClaimDemoCompanyCommandHandler depends on IDemoClaimTokenService — register
            // it directly here so Scrutor's auto-handler-registration in
            // AddApplicationServices() can validate the dependency at startup. The MCP
            // never issues claim tokens (only the API does), but the handler is still
            // pulled in by the assembly scan.
            .AddSingleton<IDemoClaimTokenService, DemoClaimTokenService>()
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
