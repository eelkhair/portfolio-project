using System.Reflection;
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
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

namespace JobBoard.Infrastructure.Diagnostics;

public static class DependencyInjection
{
    // ------------------------------------------------------------
    // OPENTELEMETRY
    // ------------------------------------------------------------
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName = "JobBoard")
    {
        services.AddSingleton<IActivityFactory, ActivitySourceFactory>();
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddScoped<IUnitOfWorkEvents, UnitOfWorkEvents>();

        var otel = services.AddOpenTelemetry();
        otel.ConfigureResource(r => r.AddService(serviceName));

        otel.WithTracing(t =>
        {
            t.AddSource(TracingFilters.Source.Name)
             .AddAspNetCoreInstrumentation(o => o.AddFilters())
             .AddEntityFrameworkCoreInstrumentation(o => o.AddFilters())
             .AddHttpClientInstrumentation(o => o.AddFilters())
             .AddOtlpExporter(exporter =>
             {
                 exporter.Endpoint = new Uri("http://192.168.1.160:4317");
             });
        });

        otel.WithMetrics(m =>
        {
            m.AddMeter(AppMetrics.Meter.Name)
             .AddAspNetCoreInstrumentation()
             .AddHttpClientInstrumentation()
             .AddOtlpExporter(exporter =>
             {
                 exporter.Endpoint = new Uri("http://192.168.1.160:4317");
             });
        });

        return services;
    }

    // ------------------------------------------------------------
    // HEALTH CHECKS
    // ------------------------------------------------------------
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DB connection missing"),
                name: "database-check",
                timeout: TimeSpan.FromSeconds(10),
                tags: new[] { "database", "critical" });

        return services;
    }

    public static WebApplication UseHealthCheckServices(this WebApplication app)
    {
        app.MapHealthChecks("/health-results", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecksUI(opt =>
        {
            opt.UIPath = "/health";
            opt.AddCustomStylesheet("wwwroot/css/healthchecks-custom.css");
        });

        return app;
    }

    // ------------------------------------------------------------
    // LOGGING (SERILOG + FILTERS)
    // ------------------------------------------------------------
    public static WebApplicationBuilder ConfigureLogging(
        this WebApplicationBuilder builder,
        string appTag)
    {
        // Clear default providers to avoid duplication
        builder.Logging.ClearProviders();

        // Reuse your existing filters
        builder.Logging.AddFilters();

        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ApplicationName", appTag)
            .Enrich.WithOpenTelemetryTraceId()
            .Enrich.WithOpenTelemetrySpanId()
            .Enrich.With(new OpenTelemetryActivityEnricher())
            .Enrich.With<OtelLinkEnricher>()
            .ApplyStandardFilters(builder.Environment)
            
            .WriteTo.Console()
            .WriteTo.Seq("http://seq")
            .WriteTo.Elasticsearch(
                ConfigureElasticSink(builder.Configuration, builder.Environment.EnvironmentName))
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }

    private static ElasticsearchSinkOptions ConfigureElasticSink(
        IConfiguration configuration,
        string environment)
    {
        return new ElasticsearchSinkOptions(
            new Uri(configuration["ElasticConfiguration:Uri"] ?? ""))
        {
            AutoRegisterTemplate = true,
            IndexFormat =
                $"{Assembly.GetEntryAssembly()?.GetName().Name?.ToLower().Replace('.', '-')}-" +
                $"{environment.ToLower().Replace('.', '-')}-" +
                $"{DateTime.UtcNow:yyyy-MM}"
        };
    }
}
