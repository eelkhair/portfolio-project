using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.Diagnostics.Observability;
using Elkhair.Common.Observability.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JobBoard.AI.Infrastructure.Diagnostics;

public static class DependencyInjection
{
    // ------------------------------------------------------------
    // OPENTELEMETRY
    // ------------------------------------------------------------
    public static IServiceCollection AddDiagnosticsServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName = "JobBoard")
    {
        services.AddSingleton<IActivityFactory, ActivitySourceFactory>();
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddScoped<IUnitOfWorkEvents, UnitOfWorkEvents>();

        var otel = services.AddOpenTelemetry();
        otel.ConfigureResource(r => r.AddService(serviceName));

        var primaryEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                              ?? "http://192.168.1.160:4317";
        var collectorEndpoint = Environment.GetEnvironmentVariable("OTEL_COLLECTOR_ENDPOINT");

        otel.WithTracing(t =>
        {
            t.SetSampler(new DaprConfigSampler())
             .AddSource(TracingFilters.Source.Name)
             .AddProcessor(new DaprInternalSpanFilter())
             .AddAspNetCoreInstrumentation(o => o.AddFilters())
             .AddEntityFrameworkCoreInstrumentation(o => o.AddFilters())
             .AddHttpClientInstrumentation(o =>
             {
                 o.AddFilters();
                 var baseFilter = o.FilterHttpRequestMessage;
                 o.FilterHttpRequestMessage = msg =>
                 {
                     // Suppress MCP SSE probe (GET / to MCP server ports)
                     if (msg.Method == HttpMethod.Get &&
                         msg.RequestUri?.AbsolutePath == "/" &&
                         msg.RequestUri?.Port is 3333 or 3334)
                         return false;
                     return baseFilter?.Invoke(msg) ?? true;
                 };
             })
             .AddOtlpExporter("primary", exporter =>
             {
                 exporter.Endpoint = new Uri(primaryEndpoint);
             });

            if (!string.IsNullOrEmpty(collectorEndpoint))
            {
                t.AddOtlpExporter("collector", exporter =>
                {
                    exporter.Endpoint = new Uri(collectorEndpoint);
                });
            }
        });

        otel.WithMetrics(m =>
        {
            m.AddMeter(AppMetrics.Meter.Name)
             .AddAspNetCoreInstrumentation()
             .AddHttpClientInstrumentation()
             .AddOtlpExporter("primary", exporter =>
             {
                 exporter.Endpoint = new Uri(primaryEndpoint);
             });

            if (!string.IsNullOrEmpty(collectorEndpoint))
            {
                m.AddOtlpExporter("collector", exporter =>
                {
                    exporter.Endpoint = new Uri(collectorEndpoint);
                });
            }
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

}
