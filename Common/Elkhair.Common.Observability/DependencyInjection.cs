using System.Reflection;
using Elkhair.Common.Observability.Middleware;
using Elkhair.Common.Observability.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
namespace Elkhair.Common.Observability;

public static class DependencyInjection
{
    // ------------------------------------------------------------
    // OPENTELEMETRY
    // ------------------------------------------------------------
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        services.AddSingleton<IActivityFactory, ActivitySourceFactory>();

        var otel = services.AddOpenTelemetry();
        otel.ConfigureResource(r => r.AddService(serviceName));

        var primaryEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                              ?? "http://192.168.1.160:4317";
        var collectorEndpoint = Environment.GetEnvironmentVariable("OTEL_COLLECTOR_ENDPOINT");

        otel.WithTracing(t =>
        {
            t.SetSampler(new DaprConfigSampler());

            t.AddSource(TracingFilters.Source.Name)
                .AddProcessor(new DaprInternalSpanFilter())
                .AddProcessor(new PiiScrubbingSpanProcessor())
                .AddAspNetCoreInstrumentation(o => o.AddFilters())
                .AddEntityFrameworkCoreInstrumentation(o => o.AddFilters())
                .AddHttpClientInstrumentation(o => o.AddFilters())
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

        return services;
    }

    public static WebApplicationBuilder ConfigureLogging(
      this WebApplicationBuilder builder,
      string appTag)
    {
        builder.Logging.AddFilters();

        var configuration = builder.Configuration;

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithProperty("@timestamp", DateTime.UtcNow)
            // FINAL: Remove all health, http ping, and authentication noise
            .Filter.ByExcluding(log =>
                (
                    log.Properties.TryGetValue("SourceContext", out var ctx) &&
                    (
                        ctx.ToString(null, System.Globalization.CultureInfo.InvariantCulture).Contains("HttpClient.health-checks") ||
                        ctx.ToString(null, System.Globalization.CultureInfo.InvariantCulture).Contains("HealthCheck") ||
                        ctx.ToString(null, System.Globalization.CultureInfo.InvariantCulture).Contains("HealthReportCollector") ||
                        (configuration.GetValue<bool>("FeatureFlags:SuppressEfCommandLogs") &&
                         ctx.ToString(null, System.Globalization.CultureInfo.InvariantCulture).Contains("EntityFrameworkCore.Database.Command"))
                    )
                )
                ||
                (
                    log.Properties.TryGetValue("Uri", out var uri) &&
                    uri.ToString(null, System.Globalization.CultureInfo.InvariantCulture).Contains("health")
                )
                ||
                (
                    log.MessageTemplate.Text.Contains("health-results") ||
                    log.MessageTemplate.Text.Contains("/health") ||
                    log.MessageTemplate.Text.Contains("Bearer was not authenticated") ||
                    log.MessageTemplate.Text.Contains("does not match a supported file type") ||
                    log.MessageTemplate.Text.Contains("POST requests are not supported") ||
                    log.MessageTemplate.Text.Contains("OPTIONS requests are not supported") ||
                    log.MessageTemplate.Text.Contains("Outbox iteration complete") ||
                    log.MessageTemplate.Text.Contains("Failed to determine the https port")
                )
            )
            .MinimumLevel.Override("Elkhair.Common.Observability.Middleware", LogEventLevel.Information)
            .MinimumLevel.Override("AdminApi", LogEventLevel.Information)
            .MinimumLevel.Override("JobApi", LogEventLevel.Information)
            .MinimumLevel.Override("CompanyApi", LogEventLevel.Information)
            .MinimumLevel.Override("UserApi", LogEventLevel.Information)
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
            // Run LAST so it catches both direct structured properties (e.g. {Email}) and
            // otel.tag.* properties copied in by OpenTelemetryActivityEnricher above.
            .Enrich.With<PiiScrubbingLogEventEnricher>()
            .ApplyStandardFilters(builder.Environment)
            .WriteTo.Console()
            .WriteTo.Elasticsearch(
                ConfigureElasticSink(builder.Configuration, builder.Environment.EnvironmentName))
            .WriteTo.OpenTelemetry(options =>
            {
                var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
                if (!string.IsNullOrEmpty(endpoint))
                {
                    options.Endpoint = endpoint;
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                }
            })
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }

    public static IApplicationBuilder UseTracingMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<TracingMiddleware>();

    private static ElasticsearchSinkOptions ConfigureElasticSink(
        IConfiguration configuration,
        string environment)
    {
        var elasticUri = configuration["ElasticConfiguration:Uri"];
        if (string.IsNullOrWhiteSpace(elasticUri) || !Uri.TryCreate(elasticUri, UriKind.Absolute, out _))
            elasticUri = "http://localhost:9200";

        return new ElasticsearchSinkOptions(new Uri(elasticUri))
        {
            AutoRegisterTemplate = true,
            IndexFormat =
                $"{Assembly.GetEntryAssembly()?.GetName().Name?.ToLower().Replace('.', '-')}-" +
                $"{environment.ToLower().Replace('.', '-')}-" +
                $"{DateTime.UtcNow:yyyy-MM}"
        };
    }
}
