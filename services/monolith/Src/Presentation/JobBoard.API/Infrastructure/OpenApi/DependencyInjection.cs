using System.Reflection;
using System.Text.Json;
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
                    .AddRouteComponents(odataRoutePrefix, EdmModel.Get());
                
            }) .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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
            //c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            //{
            //    Type = SecuritySchemeType.OAuth2,
            //    Flows = new OpenApiOAuthFlows
            //    {
            //        AuthorizationCode = new OpenApiOAuthFlow
            //        {
            //            AuthorizationUrl = new Uri($"{configuration["AzureAd:Authority"]}/oauth2/v2.0/authorize"),
            //            TokenUrl = new Uri($"{configuration["AzureAd:Authority"]}/oauth2/v2.0/token"),
            //            Scopes = new Dictionary<string, string> { { configuration["AzureAd:Scope"] ?? throw new InvalidOperationException("Missing Azure Add Scope"), "Access the API as the authenticated user" } }
            //        }
            //    }
            //});
            //var apiScope = configuration["AzureAd:Scope"] ?? throw new InvalidOperationException("Missing AzureAd:Scope in configuration.");

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
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "JobBoard API v1");
                options.SwaggerEndpoint("/swagger/odata-v1/swagger.json", "JobBoard OData v1");
                options.RoutePrefix = string.Empty;
                options.OAuthClientId(configuration["AzureAd:ClientId"]);
                options.OAuthAppName("JobBoard API - Swagger UI");
                options.OAuthUsePkce();
            });
            var clientId = configuration["Auth0:ClientId"];
            var apiScope = "LabAdmin";
            app.MapScalarApiReference("/scalar/v1", options =>
            {
                options.WithOpenApiRoutePattern("/swagger/v1/swagger.json"); 
                options.WithTitle("JobBoard v1 (Scalar)");
                options.AddAuthorizationCodeFlow("oauth2",
                    o =>
                    {
                        o.ClientId = clientId;
                        o.SelectedScopes = [apiScope];
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
                        o.SelectedScopes = [apiScope];
                        o.Pkce = Pkce.Sha256;
                    }
                );
            });
        }
        app.UseMiddleware<TraceIdMiddleware>();
        return app;
    }
}