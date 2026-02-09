using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands;
using AdminApi.Application.Commands.Interfaces;
using AdminApi.Application.Queries;
using AdminApi.Application.Queries.Interfaces;
using AdminApi.Infrastructure;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NSwag;

var builder = WebApplication.CreateBuilder(args);
    
(await builder.AddDaprServices("admin-api")).ConfigureLogging("admin-api");
builder.Services.AddOpenTelemetryServices(builder.Configuration, "admin-api");

var cfg = builder.Configuration;
var domain = cfg["Auth0:Domain"];
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

            // OAuth2 (Auth Code + PKCE) for Swagger UI via Auth0
            s.AddAuth("oauth2", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.OAuth2,
                Description = "Auth0 (Authorization Code + PKCE)",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = $"https://{domain}/authorize",
                        TokenUrl = $"https://{domain}/oauth/token",
                        Scopes = new Dictionary<string, string>
                        {
                            ["read:jobs"]  = "Read jobs",
                            ["write:jobs"] = "Create/Update jobs",
                            ["read:companies"]  = "Read companies",
                            ["write:companies"] = "Create/Update companies"
                        }
                    }
                }
            });
        };
    });
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
builder.AddCustomHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICompanyQueryService, CompanyQueryService>();
builder.Services.AddScoped<ICompanyCommandService, CompanyCommandService>();
builder.Services.AddScoped<IIndustryQueryService, IndustryQueryService>();
builder.Services.AddScoped<IJobQueryService, JobQueryService>();
builder.Services.AddScoped<IOpenAICommandService, OpenAICommandService>();
builder.Services.AddScoped<IJobCommandService, JobCommandService>();
builder.Services.AddScoped<ISettingsCommandService, SettingsCommandService>();



builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{domain}/";
        options.Audience = cfg["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://{domain}/",
            ValidateAudience = true,
            ValidAudience = cfg["Auth0:Audience"],
            ValidateLifetime = true
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                var token = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(token) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
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
app.UseFastEndpoints()
    .UseSwaggerGen(
        uiConfig: ui =>
        {
            ui.DocumentTitle = "Admin API Docs";
            ui.OAuth2Client = new()
            {
                ClientId = cfg["Auth0:SwaggerClientId"],
                ClientSecret = cfg["Auth0:SwaggerClientSecret"],
                AppName = "Admin API Swagger",
                UsePkceWithAuthorizationCodeGrant = true,
                AdditionalQueryStringParameters =
                {
                    ["audience"] = cfg["Auth0:Audience"]!
                }
            };
            // optional if you need to force it:
            // ui.OAuth2RedirectUrl = "https://localhost:5001/swagger/oauth2-redirect.html";
        });
app.UseSwaggerGen();        
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
#if DEBUG
Debugger.Launch();
#endif

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

