using Elkhair.Common.Observability;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using ReverseConnectorAPI;
using ReverseConnectorAPI.Endpoints;
using ReverseConnectorAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("reverse-connector-api"))
    .ConfigureLogging("reverse-connector-api");

builder.Services
    .AddOpenTelemetryServices(builder.Configuration, "reverse-connector-api")
    .AddApplicationServices(builder.Configuration);

builder.AddCustomHealthChecks();

var app = builder.Build();

app.UseCloudEvents();
app.MapDraftSavedEndpoint()
    .MapDraftDeletedEndpoint()
    .MapCompanyCreatedEndpoint()
    .MapCompanyUpdatedEndpoint()
    .MapJobCreatedEndpoint()
    .MapSubscribeHandler();

app.MapCustomHealthChecks(
    "/healthzEndpoint",
    "/liveness",
    UIResponseWriter.WriteHealthCheckUIResponse);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reverse Connector API v1");
});
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
