using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.API.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.Filters;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.API.Infrastructure.OpenApi;
/// <summary>
/// Dependency injection extensions for OData and Swagger services.
/// </summary>
public static class DependencyInjection
{
  
    public static IServiceCollection AddODataServices(this IServiceCollection services)
    {
        const string odataRoutePrefix = "odata";

        
        services.AddControllers(options =>
            {
                options.Conventions.Add(new KebabCaseRoutingConvention());
                options.Conventions.Add(new RouteTokenTransformerConvention(
                    new KebabCaseRoutingConvention.KebabCaseParameterTransformer()));
            })
            .AddOData(options =>
            {
                
                options

                    .Select()
                    .Filter()
                    .OrderBy()
                    .Expand()
                    .Count()
                    .SetMaxTop(100)
                    .EnableQueryFeatures()
                    .AddRouteComponents(odataRoutePrefix, EdmModel.Get(),
                        configureServices: (services) =>
                        {
                            services.AddScoped<Microsoft.OData.Json.IJsonWriterFactory>(
                                sp => new Microsoft.OData.Json.ODataJsonWriterFactory());
                        });

            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        return services;
    }

    public static IServiceCollection AddConfiguredSwagger(this IServiceCollection services, IConfiguration configuration)
    {
      
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "JobBoard API",
                Version = "v1",
                Description = "Standard RESTful endpoints."
            });

            c.SwaggerDoc("odata-v1", new OpenApiInfo
            {
                Title = "JobBoard OData API",
                Version = "v1",
                Description = "Queryable OData endpoints."
            });

            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                var relativePath = apiDesc.RelativePath;
                if (string.IsNullOrEmpty(relativePath))
                {
                    return false;
                }
                
                var isODataPath = relativePath.StartsWith("odata", StringComparison.OrdinalIgnoreCase);

                return docName switch
                {
                    "v1" => !isODataPath,
                    "odata-v1" => isODataPath,
                    _ => false
                };
            });
            var domain = configuration["Auth0:Domain"]?? string.Empty;
            var audience = configuration["Auth0:Audience"] ?? string.Empty;
            
            var scopes = new Dictionary<string, string>
            {
                { "read:jobs", "Read Jobs" },
                { "read:companies", "Read Companies" }
            };
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                //Name = "Authorization",
                //In = ParameterLocation.Header,
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        Scopes = scopes,
                        AuthorizationUrl = new Uri($"https://{domain}/authorize?audience={audience}"),
                        TokenUrl = new Uri($"https://{domain}/oauth/token")
                    }
                }
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    },
                    []
                }
            });

            c.OperationFilter<ODataQueryOperationFilter>();
            c.OperationFilter<StandardResponsesOperationFilter>();
            c.OperationFilter<TraceIdHeaderOperationFilter>();


            var httpMethodOrder = new Dictionary<string, int>
            {
                { "GET", 1 }, { "POST", 2 }, { "PUT", 3 }, { "DELETE", 4 }
            };

            c.OrderActionsBy(apiDesc =>
            {
                var sortKey = apiDesc.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
                    ? controllerActionDescriptor.ControllerName
                    : apiDesc.RelativePath?.Split('/').FirstOrDefault() ?? string.Empty;

                var httpMethod = apiDesc.HttpMethod ?? "UNKNOWN";
                var methodOrder = httpMethodOrder.GetValueOrDefault(httpMethod, 999);

                return httpMethod == "GET" ? $"{sortKey}_{methodOrder}_{apiDesc.RelativePath}" : $"{sortKey}_{methodOrder}";
            });  
            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            c.ExampleFilters();
        });
      
        services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());
        services.AddTransient<StandardResponsesOperationFilter>(sp => new StandardResponsesOperationFilter(sp));

        return services;
    }

    public static WebApplication UseConfiguredSwagger(this WebApplication app, IConfiguration configuration)
    {
        app.UseCors("AllowMyFrontendApp");
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "JobBoard API v1");
            options.SwaggerEndpoint("/swagger/odata-v1/swagger.json", "JobBoard OData v1");
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
            options.AddAuthorizationCodeFlow("oauth2",
                o =>
                {
                    o.ClientId = clientId;
                    o.ClientSecret = configuration["Auth0:SwaggerClientSecret"];
                    o.SelectedScopes = apiScopes;
                    o.Pkce = Pkce.Sha256;
                }
            );
        });

        app.MapScalarApiReference("/scalar/odata", options =>
        {
            options.WithOpenApiRoutePattern("/swagger/odata-v1/swagger.json");
            options.WithTitle("JobBoard OData v1 (Scalar)");
            options.AddAuthorizationCodeFlow("oauth2",
                o =>
                {
                    o.ClientId = clientId;
                    o.ClientSecret = configuration["Auth0:SwaggerClientSecret"];
                    o.SelectedScopes = apiScopes;
                    o.Pkce = Pkce.Sha256;
                }
            );
        });

        app.UseMiddleware<TraceIdMiddleware>();
        return app;
    }
}