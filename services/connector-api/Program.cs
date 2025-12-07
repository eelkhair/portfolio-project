using System.Diagnostics;
using ConnectorAPI.Endpoints;
using ConnectorAPI.Endpoints.Company;
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
    .AddApplicationServices();

var app = builder.Build();
    app.UseCors(CorsPolicy);
    app.MapCompanyCreatedEndpoint()
        .MapSubscribeHandler();

    app.MapServices().Run();


