using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;


namespace JobBoard.Infrastructure.Diagnostics.Observability;

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

        return builder;
    }
    
}
