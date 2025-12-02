using System.Diagnostics;
using System.Text.Json;
using ConnectorAPI.Infrastructure;
using Dapr;
using JobBoard.IntegrationEvents.Company;

var builder = WebApplication.CreateBuilder(args);

builder.AddDaprServices().ConfigureLogging("connector-api").Services.AddOpenApi();
builder.Services.AddOpenTelemetryServices(builder.Configuration, "connector-api");
builder.Services.AddHealthCheckServices(builder.Configuration)
    .AddHealthChecksUI(c => c.SetHeaderText("Connector - Health Checks")).AddInMemoryStorage();;
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/connector/company",
    [Topic("rabbitmq", "outbox-events")]
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
#if DEBUG
Debugger.Launch();
#endif

app.MapSubscribeHandler();
app.UseHealthCheckServices();
app.Run();


public class EventDto<T>(string userId, string idempotencyKey, T data)
{
    public string UserId { get; set; } = userId;

    public T Data { get; set; } = data;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public string IdempotencyKey { get; set; } = idempotencyKey;
}