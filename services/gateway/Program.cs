using System.Diagnostics;
using Elkhair.Common.Observability;
using Gateway.Api.Infrastructure;
const string CorsPolicy = "AllowJobAdmin";
#if DEBUG
Debugger.Launch();
#endif
var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("gateway"))
    .ConfigureLogging("gateway")
    .AddCustomHealthChecks()
    .Services
    .AddOpenTelemetryServices(builder.Configuration, "gateway")
    .AddApplicationServices()
    .AddReverseProxy()
    .LoadFromMemory(YarpProvider.GetRoutes(),
        YarpProvider.GetClusters(useDapr: builder.Configuration.GetValue<bool>("Gateway:UseDaprInvocation")));

var app = builder.Build();

app.UseMiddleware<TraceIdMiddleware>();
app.UseMiddleware<RoutingMiddleware>();
app.UseRouting();
app.UseCors(CorsPolicy);
app.MapReverseProxy();
app.SetupStartupServices();

app.MapGet("/", () => "Gateway is up and running!");
app.Run();