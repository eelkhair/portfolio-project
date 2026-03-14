using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Core;
using AdminApi.Infrastructure;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using JobBoard.Mcp.Common;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using NSwag;

var builder = WebApplication.CreateBuilder(args);
    
(await builder.AddDaprServices("admin-api")).ConfigureLogging("admin-api");
builder.Services.AddOpenTelemetryServices(builder.Configuration, "admin-api");

var cfg = builder.Configuration;
// Register FastEndpoints + Swagger

builder.Services.AddMessageSender();
builder.Services.AddStateManager();
builder.Services.AddSignalR();

builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Admin API";
            s.Version = "v1";

            // OAuth2 (Auth Code + PKCE) for Swagger UI via Keycloak
            var authority = cfg["Keycloak:Authority"] ?? string.Empty;
            if (!string.IsNullOrEmpty(authority))
            {
                s.AddAuth("oauth2", new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Description = "Keycloak (Authorization Code + PKCE)",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = $"{authority}/protocol/openid-connect/auth",
                            TokenUrl = $"{authority}/protocol/openid-connect/token",
                            Scopes = new Dictionary<string, string>
                            {
                                ["openid"] = "OpenID",
                                ["profile"] = "Profile",
                                ["email"] = "Email"
                            }
                        }
                    }
                });
            }
        };
    });
const string CorsPolicy = "AllowJobAdmin";
builder.Services.AddCors(options =>
{

    options.AddPolicy(CorsPolicy, p => p
        .WithOrigins(
            "http://localhost:4200",
            "https://job-admin-dev.eelkhair.net",
            "http://192.168.1.200:9000",
            "https://swagger-dev.eelkhair.net",
            "https://job-admin.eelkhair.net",
            "http://192.168.1.112:9000",
            "https://swagger.eelkhair.net")    
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("trace-id"));
});
builder.AddCustomHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAdminApiCoreServices();

builder.Services
    .AddKeycloakJwtAuth(cfg, jwt =>
    {
        // SignalR: read access_token from query string for WebSocket connections
        jwt.Events!.OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;
            var token = context.Request.Query["access_token"];

            if (!string.IsNullOrEmpty(token) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    );
});
var app = builder.Build();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();  
app.UseCloudEvents();
app.MapSubscribeHandler();
app.UseFastEndpoints(c =>
    {
        c.Endpoints.RoutePrefix = "api";
    })
    .UseSwaggerGen(
        uiConfig: ui =>
        {
            ui.DocumentTitle = "Admin API Docs";
            ui.OAuth2Client = new()
            {
                ClientId = cfg["Keycloak:SwaggerClientId"],
                AppName = "Admin API Swagger",
                UsePkceWithAuthorizationCodeGrant = true
            };
            // optional if you need to force it:
            // ui.OAuth2RedirectUrl = "https://localhost:5001/swagger/oauth2-redirect.html";
        });
app.UseSwaggerGen();        
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();

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

app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization(); 
app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

await app.RunAsync();

