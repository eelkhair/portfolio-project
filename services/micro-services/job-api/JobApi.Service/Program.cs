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

            // OAuth2 (Auth Code + PKCE) for Swagger UI via Auth0
            s.AddAuth("oauth2", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.OAuth2,
                Description = "Auth0 (Authorization Code + PKCE)",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = $"https://{builder.Configuration["Auth0:Domain"]}/authorize",
                        TokenUrl = $"https://{builder.Configuration["Auth0:Domain"]}/oauth/token",
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

const string CorsPolicy = "fe";

builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p => p
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
        options.Authority = $"https://{cfg["Auth0:Domain"]}/";
        options.Audience = cfg["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://{cfg["Auth0:Domain"]}/",
            ValidateAudience = true,
            ValidAudience = cfg["Auth0:Audience"],
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
        c.Serializer.Options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .UseSwaggerGen(
        uiConfig: ui =>
        {
            ui.DocumentTitle = "Job API Docs";
            ui.OAuth2Client = new()
            {
                ClientId = cfg["Auth0:SwaggerClientId"],
                ClientSecret = cfg["Auth0:SwaggerClientSecret"],
                AppName = "Job API Swagger",
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
app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

app.Run();

