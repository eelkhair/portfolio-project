using System.Diagnostics;
using System.Security.Claims;
using AdminApi.Application.Commands;
using AdminApi.Application.Commands.Interfaces;
using AdminApi.Application.Queries;
using AdminApi.Application.Queries.Interfaces;
using Elkhair.Dev.Common.Application;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;
// Register FastEndpoints + Swagger

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
builder.Services.AddCors();

builder.Services.AddScoped<ICompanyQueryService, CompanyQueryService>();
builder.Services.AddScoped<ICompanyCommandService, CompanyCommandService>();
builder.Services.AddScoped<IIndustryQueryService, IndustryQueryService>();

builder.ConfigureLoggingAndTracing("admin-api");

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowJobAdmin", policy =>
            policy
                .WithOrigins(
                    "http://localhost:4200",
                    "https://job-admin.eelkhair.net"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
        // add this ONLY if you send cookies/Authorization with credentials:
        //.AllowCredentials()
    );
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());
builder.Services.AddDaprClient();
var app = builder.Build();


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
app.UseCors("AllowJobAdmin");  
app.Run();

