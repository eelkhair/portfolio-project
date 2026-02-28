using System.Text.Json;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Sagas;
using ConnectorAPI.Services;
using Dapr.Client;
using Dapr.Extensions.Configuration;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.OpenApi.Models;
// ReSharper disable FunctionNeverReturns

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
        services.AddScoped<ICompanyApiClient, CompanyApiClient>();
        services.AddScoped<IJobApiClient, JobApiClient>();
        services.AddScoped<IUserApiClient, UserApiClient>();
        services.AddScoped<CompanyProvisioningSaga>();
        services.AddScoped<UpdateCompanySaga>();
        services.AddScoped<JobProvisioningSaga>();

        return services;
    }
    // ------------------------------------------------------------
    // LOGGING (SERILOG + FILTERS)
    // ------------------------------------------------------------
    
    
    public static async Task<WebApplicationBuilder> AddDaprServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Services.AddDaprClient();

        builder.Configuration.AddDaprSecretStore(
            "vault",
            new DaprClientBuilder().Build(),
            new Dictionary<string, string>()
        );

        var daprClient = new DaprClientBuilder().Build();
        var cfg = await daprClient.GetConfiguration("appconfig-" + serviceName, new List<string>());

        ApplyScopedConfig(builder.Configuration, cfg, "jobboard:config:global:", serviceName);
        ApplyScopedConfig(builder.Configuration, cfg, $"jobboard:config:{serviceName}:", serviceName);

        // Subscribe for auto-refresh
        var storeName = $"appconfig-{serviceName}";
        await daprClient.SubscribeConfiguration(storeName, new List<string> { "*" });

        _ = Task.Run(async () =>
        {
            while (true)
            {
                var config = await daprClient.GetConfiguration(storeName, new List<string>());

                foreach (var kvp in config.Items)
                {
                    if (!kvp.Key.StartsWith($"jobboard:config:{serviceName}") 
                        && !kvp.Key.StartsWith($"jobboard:config:global")) continue;
                    
                    var cleanedKey = CleanKey(kvp.Key, serviceName);
                    builder.Configuration[cleanedKey] = kvp.Value.Value;

                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        });
        return builder;
    }

    private static void ApplyScopedConfig(
        ConfigurationManager config,
        GetConfigurationResponse cfg,
        string prefix,
        string serviceName)
    {
        foreach (var item in cfg.Items.Where(k => k.Key.StartsWith(prefix)))
        {
            var cleanKey = CleanKey(item.Key, serviceName);
            config[cleanKey] = item.Value.Value;
        }
    }

    private static string CleanKey(string key, string serviceName)
    {
        key = key.Replace($"jobboard:config:{serviceName}:", "");
        key = key.Replace("jobboard:config:global:", "");
        return key;
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
