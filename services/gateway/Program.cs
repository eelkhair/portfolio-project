using System.Diagnostics;
using System.Text.RegularExpressions;
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

    // GET /jobs/{guid} (list jobs) only exists on the admin API
    var adminOnly = isMonolith
        && ctx.Request.Method == "GET"
        && ctx.Request.Path.Value is { } p
        && Regex.IsMatch(p, @"^/jobs/[0-9a-f-]+$", RegexOptions.IgnoreCase);

    string mode;
    if (adminOnly)
    {
        mode = "admin";
    }
    else
    {
        mode = isMonolith ? "monolith" : "admin";
    }
    ctx.Request.Headers["x-mode"] = mode;
    Activity.Current?.SetTag("service", mode == "monolith" ? "Monolith" : "Admin");


    await next();
});

app.UseRouting();
app.UseCors(CorsPolicy);
app.MapReverseProxy();
app.SetupStartupServices();

app.MapGet("/", () => "Gateway is up and running!");
app.Run();