using System.Diagnostics;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Http;

namespace Elkhair.Common.Observability.Observability;
public static class TracingFilters
{
    public static readonly ActivitySource Source = new("ConnectorAPi.Service");

    public static void AddFilters(this AspNetCoreTraceInstrumentationOptions options)
    {
        
        options.Filter = httpContext =>
        {
            var path = httpContext.Request.Path.ToString();
            return !path.StartsWith("/healthchecks-ui", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("healthchecks", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("/v2/track", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("/health", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("/scalar/", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("/api/health", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("swagger", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("/openapi", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("login.microsoftonline.com")&&
                   !path.Contains("/ui/resources/", StringComparison.OrdinalIgnoreCase) &&
                   !path.StartsWith("/health-results", StringComparison.OrdinalIgnoreCase); // Or just "/health" if you named it that
            
        };
    }
    public static void AddFilters(this HttpClientTraceInstrumentationOptions options)
    {
        options.FilterHttpRequestMessage = httpRequestMessage =>
        { 
            var path = httpRequestMessage.RequestUri?.AbsolutePath;
            var host = httpRequestMessage.RequestUri?.Host;
            if (!string.IsNullOrEmpty(host) && 
                host.Contains("azconfig.io", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (path != null &&
                path.Contains("dapr.proto.runtime.v1.Dapr", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
           
            return path == null 
                   || 
                   !path.Contains(".well-known")&&
                   !path.Contains("discovery/v2.0/keys")&&
                   !path.Contains("api/health")&&
                   !path.Contains("scalar")&&
                   !path.Contains("discovery/keys")&&
                   !path.Contains("/cfg-omni", StringComparison.OrdinalIgnoreCase) &&
                   !path.StartsWith("/health-results", StringComparison.OrdinalIgnoreCase)&&
                   !path.Contains("/AzureFunctionsRpcMessages.FunctionRpc/EventStream", StringComparison.OrdinalIgnoreCase)&&
                   !path.Contains("/QuickPulseService", StringComparison.OrdinalIgnoreCase);
        };
    }
    public static void AddFilters(this EntityFrameworkInstrumentationOptions options)
    {
        options.Filter = (_, command) =>
        {
            var commandText = command.CommandText;
            return !commandText.Contains("(UPDLOCK, READPAST)", StringComparison.OrdinalIgnoreCase);
        };
    }
}