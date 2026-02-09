using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.AI.API.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.AI.API.Infrastructure.OpenApi;

/// <summary>
/// Dependency injection extensions for OData and Swagger services.
/// </summary>
public static class DependencyInjection
{
 
    public static IServiceCollection AddConfiguredSwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options =>
            {
                options.Conventions.Add(new KebabCaseRoutingConvention());
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "JobBoard AI Service",
                Version = "v2",
                Description = "Standard RESTful endpoints."
            });
            c.UseInlineDefinitionsForEnums();
            var domain = configuration["Auth0:Domain"] ?? string.Empty;
            var audience = configuration["Auth0:Audience"] ?? string.Empty;

            var scopes = new Dictionary<string, string>
            {
                { "read:jobs", "Read Jobs" },
                { "read:companies", "Read Companies" }
            };

            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"https://{domain}/authorize?audience={audience}"),
                        TokenUrl = new Uri($"https://{domain}/oauth/token"),
                        Scopes = scopes
                    }
                }
            };

            c.AddSecurityDefinition("oauth2", securityScheme);

            // In OpenAPI v2, security requirements use OpenApiSecuritySchemeReference
            c.AddSecurityRequirement(_ =>
            {
                var schemeRef = new OpenApiSecuritySchemeReference("oauth2");
                var requirement = new OpenApiSecurityRequirement
                {
                    [schemeRef] = new List<string>()
                };
                return requirement;
            });

            // TraceId response header (observability)
            c.OperationFilter<TraceIdHeaderOperationFilter>();

            // Stable operation ordering
            var httpMethodOrder = new Dictionary<string, int>
            {
                { "GET", 1 },
                { "POST", 2 },
                { "PUT", 3 },
                { "DELETE", 4 }
            };

            c.OrderActionsBy(apiDesc =>
            {
                var sortKey = apiDesc.ActionDescriptor is ControllerActionDescriptor cad
                    ? cad.ControllerName
                    : apiDesc.RelativePath?.Split('/').FirstOrDefault() ?? string.Empty;

                var httpMethod = apiDesc.HttpMethod ?? "UNKNOWN";
                var methodOrder = httpMethodOrder.GetValueOrDefault(httpMethod, 999);

                return $"{sortKey}_{methodOrder}_{apiDesc.RelativePath}";
            });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        return services;
    }

    public static WebApplication UseConfiguredSwagger(
        this WebApplication app,
        IConfiguration configuration)
    {
        app.UseCors("AllowMyFrontendApp");

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "JobBoard AI Service v2");
            options.RoutePrefix = "swagger";

            options.OAuthClientId(configuration["Auth0:SwaggerClientId"]);
            options.OAuthClientSecret(configuration["Auth0:SwaggerClientSecret"]);
            options.OAuthAppName("JobBoard API - Swagger UI");
            options.OAuthUsePkce();
        });

        var clientId = configuration["Auth0:SwaggerClientId"];
        var apiScopes = new[] { "read:jobs", "read:companies" };

        app.MapScalarApiReference("/scalar", options =>
        {
            options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
            options.WithTitle("JobBoard v1 (Scalar)");
            options.AddAuthorizationCodeFlow(
                "oauth2",
                o =>
                {
                    o.ClientId = clientId;
                    o.ClientSecret = configuration["Auth0:SwaggerClientSecret"];
                    o.SelectedScopes = apiScopes;
                    o.Pkce = Pkce.Sha256;
                });
        });

        app.UseMiddleware<TraceIdMiddleware>();

        return app;
    }
}
