using AdminApi.Core;
using AdminApi.Mcp.Tools;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using JobBoard.Mcp.Common;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

// ── Dapr + Observability ─────────────────────────────────────────────────
(await builder.AddDaprCoreServices("admin-api")).ConfigureLogging("admin-api-mcp");
builder.Services.AddOpenTelemetryServices(builder.Configuration, "admin-api-mcp");

// ── Services ─────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddMessageSender();
builder.Services.AddStateManager();
builder.Services.AddAdminApiCoreServices();

// ── Auth ─────────────────────────────────────────────────────────────────
builder.Services.AddKeycloakJwtAuth(builder.Configuration);
builder.Services.AddAuthorization();

// ── MCP Server ───────────────────────────────────────────────────────────
builder.Services
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
    .WithHttpTransport(transport =>
    {
        // Stateless mode: each HTTP request gets its own session context
        // using the request's DI scope (HttpContext, IUserAccessor, etc.).
        transport.Stateless = true;
    });

// ── Health checks ────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// No UseWhen/RequireHost needed — this process owns the whole port
app.UseMiddleware<ForwardedAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.Urls.Add($"http://+:3334");
app.MapMcp();
app.MapHealthChecks("/healthz");

await app.RunAsync();
