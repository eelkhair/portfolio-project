using System.Diagnostics;
using ConnectorAPI.Endpoints;
using ConnectorAPI.Infrastructure;

#if DEBUG
Debugger.Launch();
#endif
const string CorsPolicy = "AllowJobAdmin";
var builder = WebApplication.CreateBuilder(args);
builder
    .AddDaprServices()
    .ConfigureLogging("connector-api")
    .AddCustomHealthChecks()
    .Services
    .AddOpenTelemetryServices(builder.Configuration, "connector-api")
    .AddApplicationServices(CorsPolicy);

var app = builder.Build();
    app.UseCors(CorsPolicy);
    app.SetupCompanyEndpoints()
        .MapSubscribeHandler();

    app.MapServices().Run();


