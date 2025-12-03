using System.Diagnostics;
using System.Text.Json;
using ConnectorAPI.Infrastructure;
using Dapr;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using JobBoard.IntegrationEvents.Company;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
Debugger.Launch();
#endif
const string CorsPolicy = "AllowJobAdmin";
builder.Services.AddCors(options =>
{

    options.AddPolicy(CorsPolicy, p => p
        .WithOrigins(
            "http://localhost:4200",
            "https://job-admin.eelkhair.net",
            "http://192.168.1.112:9000",
            "https://swagger.eelkhair.net")    
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("trace-id"));
});
builder.AddDaprServices()
    .ConfigureLogging("connector-api")
    .AddCustomHealthChecks();

builder.Services.AddOpenTelemetryServices(builder.Configuration, "connector-api");

// ------------------------------
// ✅ MOVE THIS BEFORE builder.Build()
// ------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Connector API",
        Version = "v1",
        Description = "Standard RESTful endpoints."
    });
});
// ------------------------------

var app = builder.Build();
app.UseCors(CorsPolicy);
// Map your endpoints
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

app.MapCustomHealthChecks(
    "/healthzEndpoint",
    "/liveness",
    UIResponseWriter.WriteHealthCheckUIResponse);

// Swagger UI must come after endpoints
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Connector API v1");
});
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

public class EventDto<T>(string userId, string idempotencyKey, T data)
{
    public string UserId { get; set; } = userId;
    public T Data { get; set; } = data;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; set; } = idempotencyKey;
}
