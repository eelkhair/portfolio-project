using System.Diagnostics;
using System.Security.Claims;
using Elkhair.Common.Observability;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using FastEndpoints;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using UserApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

using Microsoft.IdentityModel.Tokens;
using NSwag;
using UserApi.Application.Commands;
using UserApi.Application.Commands.Interfaces;
using UserApi.Infrastructure;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;


var builder = WebApplication.CreateBuilder(args);
   (await builder.AddDaprServices("user-api")).ConfigureLogging("user-api");

builder.Services.AddOpenTelemetryServices(builder.Configuration, "user-api");
var cfg = builder.Configuration;
// Register FastEndpoints + Swagger
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "User API";
            s.Version = "v1";

            // OAuth2 (Auth Code + PKCE) for Swagger UI via Keycloak
            var authority = builder.Configuration["Keycloak:Authority"] ?? string.Empty;
            if (!string.IsNullOrEmpty(authority))
            {
                s.AddAuth("oauth2", new NSwag.OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Description = "Keycloak (Authorization Code + PKCE)",
                    Flows = new NSwag.OpenApiOAuthFlows
                    {
                        AuthorizationCode = new NSwag.OpenApiOAuthFlow
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
builder.AddCustomHealthChecks();

builder.Services.AddDaprClient();

builder.Services.AddHttpClient("keycloak");
builder.Services.AddScoped<IKeycloakCommandService, KeycloakCommandService>();
builder.Services.AddSingleton<IKeycloakTokenService, KeycloakTokenService>();
builder.Services.AddTransient<IKeycloakFactory, DefaultKeycloakFactory>();
builder.Services.AddHostedService<KeycloakTokenStartupService>();
builder.Services.AddMessageSender();
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserDbContext"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, "Users");
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
});
builder.Services.AddScoped<IUserDbContext, UserDbContext>();
builder.Services.AddScoped<ICompanyCommandService, CompanyCommandService>();

// Add Authorization support (even if not using yet)

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

builder.Services.AddHttpContextAccessor();
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
    })
    .UseSwaggerGen(
        uiConfig: ui =>
        {
            ui.DocumentTitle = "User API Docs";
            ui.OAuth2Client = new()
            {
                ClientId = cfg["Keycloak:SwaggerClientId"],
                AppName = "User API Swagger",
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
app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);

app.Run();

