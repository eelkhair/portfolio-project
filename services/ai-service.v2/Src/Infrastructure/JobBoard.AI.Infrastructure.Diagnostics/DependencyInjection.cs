using System.Reflection;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Diagnostics.Observability;
using JobBoard.Infrastructure.Diagnostics.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

namespace JobBoard.AI.Infrastructure.Diagnostics;

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
             .AddAspNetCoreInstrumentation(o => o.AddFilters() )
             .AddEntityFrameworkCoreInstrumentation(o => o.AddFilters())
             .AddHttpClientInstrumentation(o => o.AddFilters())
             .AddZipkinExporter()
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
                configuration.GetConnectionString("Monolith")
                ?? throw new InvalidOperationException("DB connection missing"),
                name: "database-check",
                timeout: TimeSpan.FromSeconds(10),
                tags: new[] { "database", "critical" });

        return services;
    }

    // ------------------------------------------------------------
    // LOGGING (SERILOG + FILTERS)
    // ------------------------------------------------------------
    public static WebApplicationBuilder ConfigureLogging(
        this WebApplicationBuilder builder,
        string appTag)
    {
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddFilters();

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithProperty("@timestamp", DateTime.UtcNow)
            // FINAL: Remove all health, http ping, and authentication noise
            .Filter.ByExcluding(log =>
                (
                    log.Properties.TryGetValue("SourceContext", out var ctx) &&
                    (
                        ctx.ToString().Contains("HttpClient.health-checks") ||
                        ctx.ToString().Contains("HealthCheck") ||
                        ctx.ToString().Contains("HealthReportCollector")
                    )
                )
                ||
                (
                    log.Properties.TryGetValue("Uri", out var uri) &&
                    uri.ToString().Contains("health")
                )
                ||
                (
                    log.MessageTemplate.Text.Contains("health-results") ||
                    log.MessageTemplate.Text.Contains("/health") ||
                    log.MessageTemplate.Text.Contains("Bearer was not authenticated") ||
                    log.MessageTemplate.Text.Contains("does not match a supported file type") ||
                    log.MessageTemplate.Text.Contains("POST requests are not supported") ||
                    log.MessageTemplate.Text.Contains("OPTIONS requests are not supported")
                )
            )
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.DataProtection", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Error)
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.With<ElasticTimestampEnricher>()  
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
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
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
