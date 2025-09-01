using System.Security.Claims;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobApi.Data;
using JobApi.Infrastructure.Data;
using JobApi.Presentation.Endpoints.Jobs.Create;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

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
            s.Title = "Job API";
            s.Version = "v1";

            // OAuth2 (Auth Code + PKCE) for Swagger UI via Auth0
            s.AddAuth("oauth2", new NSwag.OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.OAuth2,
                Description = "Auth0 (Authorization Code + PKCE)",
                Flows = new NSwag.OpenApiOAuthFlows
                {
                    AuthorizationCode = new NSwag.OpenApiOAuthFlow
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
builder.Services.AddScoped<IJobDbContext, JobDbContext>();
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal());

var app = builder.Build();
app.UseCors(policy => policy.AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("http://localhost:4200")
    .WithOrigins("https://job-admin.eelkhair.net")
);

app.UseAuthentication();    
app.UseAuthorization();     
app.UseFastEndpoints()
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
app.Run();

