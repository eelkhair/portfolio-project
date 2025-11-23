using HealthChecks.UI.Client;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Infrastructure.Diagnostics.HealthChechs;
using JobBoard.Infrastructure.Diagnostics.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JobBoard.Infrastructure.Diagnostics;

public static class DependencyInjection
{
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration, 
         
        string serviceName = "JobBoard")
    {
        services.AddSingleton<IActivityFactory, ActivitySourceFactory>();
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddScoped<IUnitOfWorkEvents, UnitOfWorkEvents>();
 
        var openTelemetry = services.AddOpenTelemetry();
        openTelemetry.ConfigureResource(resource => resource.AddService(serviceName));
        openTelemetry.WithTracing(tracerBuilder =>
        {
            tracerBuilder
                .AddSource(TracingFilters.Source.Name)
                .AddAspNetCoreInstrumentation(options => options.AddFilters())
                .AddEntityFrameworkCoreInstrumentation(options => options.AddFilters())

                .AddHttpClientInstrumentation(options => options.AddFilters())
                .AddOtlpExporter(c => c.Endpoint = new Uri("http://localhost:17011"));
        });
        openTelemetry.WithMetrics(meterProviderBuilder =>
        {
            meterProviderBuilder
                .AddMeter(AppMetrics.Meter.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(c => c.Endpoint = new Uri("http://localhost:17011"));
        });

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddOpenTelemetry(options =>
            {
                options.AddOtlpExporter(c => c.Endpoint = new Uri("http://localhost:17011"));
                
            });
            loggingBuilder.AddFilters();
        });
        return services;
    }
    
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection") ??
                                  throw new InvalidOperationException("Database connection string is not configured."),
                name: "database-check",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["database", "critical"])
            
            .AddCheck<KafkaHealthCheck>(
                name: "kafka-connection-check",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["messaging", "critical"]);

        return services;
    }
    
    public static WebApplication UseHealthCheckServices(this WebApplication app)
    {
        app.MapHealthChecks("/health-results", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health"; 
            options.AddCustomStylesheet("wwwroot/css/healthchecks-custom.css");
        });
        return app;
    }
}