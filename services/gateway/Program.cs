using System.Diagnostics;
using Elkhair.Common.Observability;
using Gateway.Api.Infrastructure;
using JobBoard.Infrastructure.RedisConfig;
using JobBoard.Infrastructure.Vault;
const string CorsPolicy = "AllowJobAdmin";

var builder = WebApplication.CreateBuilder(args);
var isAspire = builder.Configuration["ASPIRE_MODE"] == "true";
if (!isAspire) builder.AddVaultSecrets("gateway");
(await builder.AddRedisConfiguration("gateway", TimeSpan.FromSeconds(8)))
    .ConfigureLogging("gateway")
    .AddCustomHealthChecks(isAspire)
    .Services
    .AddOpenTelemetryServices(builder.Configuration, "gateway")
    .AddApplicationServices()
    .AddReverseProxy()
    .LoadFromMemory(YarpProvider.GetRoutes(),
        YarpProvider.GetClusters(builder.Configuration));

var app = builder.Build();

app.UseWebSockets();
app.UseMiddleware<TraceIdMiddleware>();
app.UseMiddleware<RoutingMiddleware>();
app.UseRouting();
app.UseCors(CorsPolicy);
app.MapReverseProxy();
app.SetupStartupServices();

app.MapGet("/", () => "Gateway is up and running!");
app.Run();