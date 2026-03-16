using Elkhair.Common.Observability;
using ReverseConnectorAPI;
using ReverseConnectorAPI.Endpoints;

var builder = WebApplication.CreateBuilder(args);
(await builder.AddDaprServices("reverse-connector-api"))
    .ConfigureLogging("reverse-connector-api");

builder.Services
    .AddOpenTelemetryServices(builder.Configuration, "reverse-connector-api")
    .AddApplicationServices(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCloudEvents();
app.MapDraftSavedEndpoint()
    .MapDraftDeletedEndpoint()
    .MapCompanyCreatedEndpoint()
    .MapCompanyUpdatedEndpoint()
    .MapJobCreatedEndpoint()
    .MapSubscribeHandler();

app.MapHealthChecks("/healthzEndpoint");
app.MapHealthChecks("/liveness");

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reverse Connector API v1");
});
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();

app.Run();