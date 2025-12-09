using System.Text.Json;
using ConnectorAPI.Interfaces;
using ConnectorAPI.Services;
using Dapr.Client;
using Dapr.Extensions.Configuration;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.OpenApi.Models;

namespace ConnectorAPI.Infrastructure;

public static class DependencyInjection
{


    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string corsPolicy = "AllowJobAdmin")
    {
       
        services.AddCors(options =>
        {
            options.AddPolicy(corsPolicy, p => p
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
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Connector API",
                Version = "v1",
                Description = "Standard RESTful endpoints."
            });
        });
        
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        
        services.AddScoped<IMonolithClient, MonolithOClient>();
        services.AddScoped<IAdminApiClient, AdminApiClient>();
        return services;
    }
    // ------------------------------------------------------------
    // LOGGING (SERILOG + FILTERS)
    // ------------------------------------------------------------
    
    public static WebApplicationBuilder AddDaprServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDaprClient(); 
        builder.Configuration.AddDaprSecretStore("vault", 
            new DaprClientBuilder().Build(), 
            new Dictionary<string, string>
            {
                { "secret/data/portfolio/monolith", "Monolith" }
            });
        
        return builder;
    }

    public static WebApplication MapServices(this WebApplication app)
    {
        app.UseCloudEvents();
        app.MapCustomHealthChecks(
            "/healthzEndpoint",
            "/liveness",
            UIResponseWriter.WriteHealthCheckUIResponse);
        
        // Swagger UI must come after endpoints
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Connector API v1");
        });
        app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
       
        return app;
    }
}
