using System.Diagnostics;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Http;

namespace Elkhair.Common.Observability.Observability;
public static class TracingFilters
{
    public static readonly ActivitySource Source = new("JobBoard");

    public static void AddFilters(this AspNetCoreTraceInstrumentationOptions options)
    {
        
        options.Filter = httpContext =>
        {
            var path = httpContext.Request.Path.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            // Drop all Dapr sidecar probes (Go-http-client)
            if (userAgent.StartsWith("Go-http-client", StringComparison.OrdinalIgnoreCase))
                return false;

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
                   !path.StartsWith("/health-results", StringComparison.OrdinalIgnoreCase) &&
                   !path.Equals("/dapr/config", StringComparison.OrdinalIgnoreCase) &&
                   !path.Equals("/liveness", StringComparison.OrdinalIgnoreCase) &&
                   !path.Equals("/readiness", StringComparison.OrdinalIgnoreCase);
        };
    }
    public static void AddFilters(this HttpClientTraceInstrumentationOptions options)
    {
        options.FilterHttpRequestMessage = httpRequestMessage =>
        {
            var path = httpRequestMessage.RequestUri?.AbsolutePath;
            var host = httpRequestMessage.RequestUri?.Host;
            var port = httpRequestMessage.RequestUri?.Port;

            if (!string.IsNullOrEmpty(host) &&
                host.Contains("azconfig.io", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path != null &&
                path.Contains("dapr.proto.runtime.v1.Dapr", StringComparison.OrdinalIgnoreCase))
                return false;

            // Vault health checks
            if (path != null &&
                path.Equals("/v1/sys/health", StringComparison.OrdinalIgnoreCase))
                return false;

            // Dapr sidecar port (liveness / readiness / healthz)
            if (port == 3333 || port == 3500)
                return false;

            // Dapr health endpoints on any port
            if (path != null &&
                (path.Contains("/healthz", StringComparison.OrdinalIgnoreCase) ||
                 path.Equals("/liveness", StringComparison.OrdinalIgnoreCase) ||
                 path.Equals("/readiness", StringComparison.OrdinalIgnoreCase)))
                return false;

            // Elasticsearch (Serilog sink)
            if (port == 9200)
                return false;

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
                   !path.Contains("/QuickPulseService", StringComparison.OrdinalIgnoreCase) &&
                   !path.Equals("/v1/models", StringComparison.OrdinalIgnoreCase) &&
                   !path.Contains("/openai/models", StringComparison.OrdinalIgnoreCase);
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