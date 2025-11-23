
using Microsoft.Extensions.Logging;

namespace JobBoard.Infrastructure.Diagnostics.Observability;

public static class Logging
{
    public static void AddFilters(this ILoggingBuilder loggingBuilder)
    {
         loggingBuilder.AddFilter("JobBoard.Infrastructure.Diagnostics", LogLevel.Information);
         if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
         { 
             //    loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
         }             
         loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error);
         loggingBuilder.AddFilter("System.Net.Http.HttpClient.health-checks", LogLevel.Warning);
         loggingBuilder.AddFilter("HealthChecks.UI", LogLevel.Warning);
         loggingBuilder.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
         loggingBuilder.AddFilter("Microsoft.Extensions.Http", LogLevel.Information);
         loggingBuilder.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
         loggingBuilder.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
         loggingBuilder.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure.ObjectResultExecutor", LogLevel.Warning);

         loggingBuilder.AddFilter("System.Net.Http.HttpClient.OtlpMetricExporter", LogLevel.Warning);
         loggingBuilder.AddFilter("System.Net.Http.HttpClient.OtlpTraceExporter", LogLevel.Warning);
         loggingBuilder.AddFilter("Microsoft.IdentityMode", LogLevel.Warning);
         
         loggingBuilder.AddFilter("Default", LogLevel.Warning);
    }
    
}
