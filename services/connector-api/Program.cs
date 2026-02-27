using System.Diagnostics;
using ConnectorAPI.Endpoints.Company;
using ConnectorAPI.Endpoints.Job;
using ConnectorAPI.Infrastructure;
using Elkhair.Common.Observability;

#if DEBUG
Debugger.Launch();
#endif
const string CorsPolicy = "AllowJobAdmin";
var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("connector-api"))
    .ConfigureLogging("connector-api")
    .AddCustomHealthChecks()
    .Services
    .AddOpenTelemetryServices(builder.Configuration, "connector-api")
    .AddApplicationServices();

var app = builder.Build();
    app.UseCors(CorsPolicy);
    app.MapCompanyCreatedEndpoint()
        .MapJobCreatedEndpoint()
        .MapSubscribeHandler();

    app.MapServices().Run();


