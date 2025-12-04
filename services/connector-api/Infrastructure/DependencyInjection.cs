using System.Reflection;
using ConnectorAPI.Infrastructure.Observability;
using Dapr.Client;
using Dapr.Extensions.Configuration;
using HealthChecks.UI.Client;
using JobBoard.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

namespace ConnectorAPI.Infrastructure;

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

      

        return services;
    }

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
