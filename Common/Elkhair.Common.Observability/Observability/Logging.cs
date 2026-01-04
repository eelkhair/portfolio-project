using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Elkhair.Common.Observability.Observability;

public static class LoggingFilters
{
    public static LoggerConfiguration ApplyStandardFilters(
        this LoggerConfiguration logger, 
        IHostEnvironment env)
    {
        // ------------------------------------------------------------
        // EF Core noise reduction
        // ------------------------------------------------------------
        logger = logger
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error);

        // ------------------------------------------------------------
        // ASP.NET Core noise
        // ------------------------------------------------------------
        logger = logger
            .MinimumLevel.Override("Microsoft.Extensions.Hosting", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogEventLevel.Warning);

        // ------------------------------------------------------------
        // OTEL Exporters
        // ------------------------------------------------------------
        logger = logger
            .MinimumLevel.Override("System.Net.Http.HttpClient.OtlpMetricExporter", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient.OtlpTraceExporter", LogEventLevel.Warning);

        // ------------------------------------------------------------
        // Health Check spam
        // ------------------------------------------------------------
        logger = logger
            .Filter.ByExcluding(log =>
                log.Properties.TryGetValue("RequestPath", out var path) &&
                path.ToString().Contains("/health"));

        // ------------------------------------------------------------
        // Development overrides (verbose EF)
        // ------------------------------------------------------------
        if (env.IsDevelopment())
        {
            logger = logger
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information);
        }

        return logger;
    }
    public static ILoggingBuilder AddFilters(this ILoggingBuilder builder)
    {
        
        builder.AddFilter("Microsoft", LogLevel.Error);
        builder.AddFilter("System", LogLevel.Error);

        builder.AddFilter("Microsoft.AspNetCore", LogLevel.Error);
        builder.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Critical);
        builder.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Critical);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.ModelBinding", LogLevel.Critical);
        builder.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Error);
        builder.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Error);
        builder.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Error);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure", LogLevel.Error);
        builder.AddFilter("Microsoft.AspNetCore.HttpsPolicy", LogLevel.Error);
        // Infrastructure logs
        builder.AddFilter("JobBoard.Infrastructure.Diagnostics", LogLevel.Information);

        // EF Core (quiet unless something is wrong)
        builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error);

        // Health checks
        builder.AddFilter("System.Net.Http.HttpClient.health-checks", LogLevel.Warning);
        builder.AddFilter("HealthChecks.UI", LogLevel.Warning);

        // Host + Routing + MVC infrastructure
        builder.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
        builder.AddFilter("Microsoft.Extensions.Http", LogLevel.Information);
        builder.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Cors.Infrastructure.CorsService", LogLevel.Error);

        // OTEL Exporters (they get noisy)
        builder.AddFilter("System.Net.Http.HttpClient.OtlpMetricExporter", LogLevel.Warning);
        builder.AddFilter("System.Net.Http.HttpClient.OtlpTraceExporter", LogLevel.Warning);

        // Auth noise
        builder.AddFilter("Microsoft.IdentityModel", LogLevel.Warning);

        // Default: warnings or higher
        builder.AddFilter("Default", LogLevel.Warning);
        
        builder.AddFilter("JobBoard.Application", LogLevel.Information);
        builder.AddFilter("JobBoard.Infrastructure", LogLevel.Information);

        builder.AddFilter("HealthChecks.UI", LogLevel.Warning);
        builder.AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks", LogLevel.Warning);
        builder.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
        builder.AddFilter("System.Net.Http.HttpClient.health-checks", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Diagnostics.HealthChecks", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Cors", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.ModelBinding", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.Filters", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Mvc.Controllers", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Server", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.AddFilter("Dapr", LogLevel.Warning);
        builder.AddFilter("Dapr.Client", LogLevel.Warning);
        builder.AddFilter("Dapr.Runtime", LogLevel.Warning);
        builder.AddFilter("Dapr.Actors", LogLevel.Warning);
        builder.AddFilter("Dapr.Sidecar", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Server", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

// Dapr
        builder.AddFilter("Dapr", LogLevel.Warning);
        builder.AddFilter("Dapr.Client", LogLevel.Warning);
        builder.AddFilter("Dapr.Runtime", LogLevel.Warning);
        builder.AddFilter("Dapr.Actors", LogLevel.Warning);
        builder.AddFilter("Dapr.Sidecar", LogLevel.Warning);
        builder.AddFilter("Dapr.Placement", LogLevel.Warning);
        builder.AddFilter("Dapr.Contrib", LogLevel.Warning);

// Hide Dapr → ASP.NET Core internal pings
        builder.AddFilter("Microsoft.AspNetCore.Hosting.Internal", LogLevel.Warning);
        builder.AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.Warning);
  
        return builder;
    }
    
}
public class ElasticTimestampEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        logEvent.AddOrUpdateProperty(
            factory.CreateProperty("@timestamp", logEvent.Timestamp.UtcDateTime));
    }
}