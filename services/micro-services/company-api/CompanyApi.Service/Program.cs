using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using CompanyApi.Application.Commands;
using CompanyApi.Application.Commands.Interfaces;
using CompanyApi.Application.Queries;
using CompanyApi.Application.Queries.Interfaces;
using CompanyApi.Infrastructure;
using CompanyApi.Infrastructure.Data;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

using Microsoft.IdentityModel.Tokens;
using NSwag;

var builder = WebApplication.CreateBuilder(args);
   ( await builder.AddDaprServices("company-api")).ConfigureLogging("company-api");

var cfg = builder.Configuration;
builder.Services.AddOpenTelemetryServices(builder.Configuration, "company-api");
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Company API";
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
builder.Services.AddScoped<ICompanyQueryService, CompanyQueryService>();
builder.Services.AddScoped<IIndustryQueryService, IndustryQueryService>();
builder.Services.AddScoped<ICompanyCommandService, CompanyCommandService>();
builder.Services.AddMessageSender();
builder.AddCustomHealthChecks();
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(Assembly.GetExecutingAssembly());   // <-- finds Mappers : IRegister

builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddDbContext<CompanyDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("CompanyDbContext"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Companies");
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
builder.Services.AddScoped<ICompanyDbContext, CompanyDbContext>();
// Add Authorization support (even if not using yet)

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
app.UseFastEndpoints()
    .UseSwaggerGen(
        uiConfig: ui =>
        {
            ui.DocumentTitle = "Company API Docs";
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

await app.RunAsync();

