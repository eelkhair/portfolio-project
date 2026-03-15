using HealthChecks.UI.Client;
using JobBoard.API.Mcp.Infrastructure;
using JobBoard.API.Mcp.Tools;
using JobBoard.Application;
using JobBoard.HealthChecks;
using JobBoard.Mcp.Common;
using JobBoard.Infrastructure.BlobStorage;
using JobBoard.Infrastructure.Diagnostics;
using JobBoard.Infrastructure.HttpClients;
using JobBoard.Infrastructure.Messaging;
using JobBoard.Infrastructure.Outbox;
using JobBoard.Infrastructure.Persistence;
using JobBoard.Infrastructure.RedisConfig;
using JobBoard.Infrastructure.Smtp;
using JobBoard.Infrastructure.Vault;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

// ── Vault + Redis config ────────────────────────────────────────────────
builder.AddVaultSecrets("monolith");
(await builder.AddRedisConfiguration("monolith-mcp", TimeSpan.FromSeconds(5)))
    .ConfigureLogging("monolith-mcp");

// ── Infrastructure services (same as API, minus OData/Swagger/SignalR) ──
builder.Services
    .AddApplicationServices()
    .AddPersistenceServices(builder.Configuration)
    .AddOutboxServices()
    .AddMassTransitMessaging(builder.Configuration)
    .AddSmtpServices(builder.Configuration)
    .AddBlobStorageServices(builder.Configuration)
    .AddAiServiceHttpClient(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<IUserAccessor, HttpUserAccessor>()
    .AddOpenTelemetryServices(builder.Configuration, "monolith-mcp");

// ── Auth ─────────────────────────────────────────────────────────────────
builder.Services.AddKeycloakJwtAuth(builder.Configuration);
builder.Services.AddAuthorization();

// ── MCP Server ───────────────────────────────────────────────────────────
builder.Services.AddScoped<HandlerDispatcher>();
builder.Services
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
    .WithHttpTransport(transport =>
    {
        // Stateless mode: each HTTP request gets its own session context
        // using the request's DI scope (HttpContext, IUserAccessor, etc.).
        transport.Stateless = true;
    });

// ── CORS (for MCP Inspector) ────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Health checks ────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// No UseWhen/RequireHost needed — this process owns the whole port
app.UseCors();
app.UseMiddleware<ForwardedAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.Urls.Add($"http://+:3333");
app.MapMcp();
app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

await app.RunAsync();
