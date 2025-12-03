using System.Diagnostics;
using System.Text.Json;
using ConnectorAPI.Infrastructure;
using Dapr;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using JobBoard.IntegrationEvents.Company;

var builder = WebApplication.CreateBuilder(args);
#if DEBUG
Debugger.Launch();
#endif
builder.AddDaprServices().ConfigureLogging("connector-api").AddCustomHealthChecks().Services.AddOpenApi();
builder.Services.AddOpenTelemetryServices(builder.Configuration, "connector-api");

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/connector/company",
    [Topic("rabbitmq.pubsub", "outbox-events")]
    async (
        JsonElement e,
        HttpContext http,
        ILoggerFactory loggerFactory) =>
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;
        var logger = loggerFactory.CreateLogger("CompanyCreatedV1Event");
        logger.LogInformation("Received event {TraceId}", traceId);
        var payload = e.GetProperty("data")
            .Deserialize<CompanyCreatedV1Event>();
        return Results.Ok();
    });


app.MapSubscribeHandler();
app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);
app.Run();


public class EventDto<T>(string userId, string idempotencyKey, T data)
{
    public string UserId { get; set; } = userId;

    public T Data { get; set; } = data;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public string IdempotencyKey { get; set; } = idempotencyKey;
}