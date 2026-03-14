using JobBoard.AI.Application;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Notifications;
using JobBoard.AI.Infrastructure.AI;
using JobBoard.AI.Infrastructure.Configuration;
using JobBoard.AI.Infrastructure.Dapr;
using JobBoard.AI.Infrastructure.Diagnostics;
using JobBoard.AI.Infrastructure.HttpClients;
using JobBoard.AI.Infrastructure.Persistence;
using JobBoard.AI.MCP.Micro.Infrastructure;
using JobBoard.AI.MCP.Micro.Tools.Admin;
using Azure.Storage.Blobs;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

(await builder.AddDaprServices("ai-service-mcp-micro"))
    .ConfigureLogging("ai-service-mcp-micro")
    .AddCustomHealthChecks()
    .Services
    .AddApplicationServices()
    .AddConfigurationServices(builder.Configuration)
    .AddAiServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration)
    .AddOpenTelemetryServices(builder.Configuration, "ai-service-mcp-micro");

builder.Services.AddMonolithHttpClient(builder.Configuration);

var blobConnectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

// MCP-specific overrides (after shared DI so they replace HttpUserAccessor)
builder.Services.AddSingleton<KeycloakTokenService>();
builder.Services.AddSingleton<IUserAccessor, McpUserAccessor>();
builder.Services.AddSingleton<IAiNotificationHub, NullNotificationHub>();

// MCP server + tool discovery
var mcpBuilder = builder.Services
    .AddMcpServer(o => o.ServerInfo = new Implementation
    {
        Name = "ai-service-mcp-micro",
        Version = "1.0.0"
    })
    .WithTools<CompanyTools>()
    .WithTools<IndustryTools>()
    .WithTools<JobTools>()
    .WithTools<DraftTools>()
    .WithTools<SystemTools>();

var useStdio = args.Contains("--stdio");
if (useStdio)
    mcpBuilder.WithStdioServerTransport();
else
    mcpBuilder.WithHttpTransport();

builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

var app = builder.Build();

if (!useStdio)
{
    app.UseMiddleware<UserContextMiddleware>();
    app.MapMcp();
    app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);
    app.Urls.Add($"http://+:3334");
}

app.Run();
