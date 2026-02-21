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
    .LoadFromMemory(YarpProvider.GetRoutes(), YarpProvider.GetClusters());

var app = builder.Build();
app.Use(async (context, next) =>
{
    // Get the current span and its traceid
    var span = Activity.Current;
    var traceId = span?.TraceId.ToString();

    // Add the traceid to the response headers
    context.Response.Headers.Append("trace-id", traceId);

    // Call the next middleware in the pipeline
    await next();
});
app.Use(async (ctx, next) =>
{

    if (ctx.Request.Path.StartsWithSegments("/ai/v2")
        || ctx.Request.Path.StartsWithSegments("/dapr/config")
        || ctx.Request.Path.StartsWithSegments("/dapr/subscribe"))
    {
        if(ctx.Request.Path.StartsWithSegments("/ai/v2"))
            Activity.Current?.SetTag("service", "AI V2");

        await next();
        return;
    }

    var isMonolith = builder.Configuration.GetValue<bool>("FeatureFlags:Monolith");
    ctx.Request.Headers["x-mode"] = isMonolith ? "monolith" : "admin";
    Activity.Current?.SetTag("service", isMonolith ? "Monolith" : "Admin");


    await next();
});

app.UseRouting();
app.UseCors(CorsPolicy);
app.MapReverseProxy();
app.SetupStartupServices();

app.MapGet("/", () => "Gateway is up and running!");
app.Run();