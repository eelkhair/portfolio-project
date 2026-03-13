using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using Elkhair.Common.Observability;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using HealthChecks.UI.Client;
using JobApi.Application;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;

using JobApi.Features.Jobs.Create;
using JobApi.Infrastructure;
using JobApi.Infrastructure.Data;
using JobBoard.HealthChecks;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

using Microsoft.IdentityModel.Tokens;
using NSwag;

var builder = WebApplication.CreateBuilder(args);
    (await builder.AddDaprServices("job-api")).ConfigureLogging("job-api");
builder.Services.AddOpenTelemetryServices(builder.Configuration, "job-api");
var cfg = builder.Configuration;
// Register FastEndpoints + Swagger
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(Assembly.GetExecutingAssembly());

builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Job API";
            s.Version = "v1";

            // OAuth2 (Auth Code + PKCE) for Swagger UI via Keycloak
            var authority = builder.Configuration["Keycloak:Authority"] ?? string.Empty;
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

const string CorsPolicy = "fe";

builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p => p
        .WithOrigins(
            "http://localhost:4200",
            "https://job-admin-dev.eelkhair.net",
            "https://jobs-dev.eelkhair.net",
            "https://job-dev.eelkhair.net",
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
builder.Services.AddScoped<IValidator<CreateJobRequest>, CreateJobValidator>();

builder.Services.AddDbContext<JobDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobDbContext"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Jobs");
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
builder.AddCustomHealthChecks();
builder.Services.AddScoped<IJobDbContext, JobDbContext>();
builder.Services.AddScoped<ICompanyCommandService, CompanyCommandService>();
builder.Services.AddScoped<IJobQueryService, JobQueryService>();
builder.Services.AddScoped<IJobCommandService, JobCommandService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.Authority = cfg["Keycloak:Authority"];
        options.Audience = cfg["Keycloak:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());
builder.Services.AddDaprClient();
var app = builder.Build();

app.UseCors(CorsPolicy);

app.UseAuthentication();    
app.UseAuthorization();  
app.UseCloudEvents();
app.MapSubscribeHandler();
app.UseFastEndpoints(c =>
    {
        c.Endpoints.RoutePrefix = "api";
        c.Serializer.Options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .UseSwaggerGen(
        uiConfig: ui =>
        {
            ui.DocumentTitle = "Job API Docs";
            ui.OAuth2Client = new()
            {
                ClientId = cfg["Keycloak:SwaggerClientId"],
                AppName = "Job API Swagger",
                UsePkceWithAuthorizationCodeGrant = true
            };
            // optional if you need to force it:
            // ui.OAuth2RedirectUrl = "https://localhost:5001/swagger/oauth2-redirect.html";
        });
app.UseSwaggerGen();        
app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
// #if DEBUG
// Debugger.Launch();
// #endif

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
app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

app.Run();

