using HealthChecks.UI.Client;
using JobBoard.HealthChecks;

namespace Gateway.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string corsPolicy = "AllowJobAdmin")
    {

        services.AddCors(options =>
        {
            options.AddPolicy(corsPolicy, p => p
                .WithOrigins(
                    "http://localhost:4200",
                    "http://localhost:3000",
                    "https://job-admin-dev.eelkhair.net",
                    "https://jobs-dev.eelkhair.net",
                    "https://jobs.eelkhair.net",
                    "https://job-dev.eelkhair.net",
                    "http://192.168.1.200:9000",
                    "https://swagger-dev.eelkhair.net",
                    "https://job-admin.eelkhair.net",
                    "http://192.168.1.112:9000",
                    "https://swagger.eelkhair.net")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("trace-id", "x-trace-id"));
        });
        return services;
    }

    public static WebApplication SetupStartupServices(this WebApplication app)
    {
        app.MapCustomHealthChecks(
            "/healthzEndpoint",
            "/liveness",
            UIResponseWriter.WriteHealthCheckUIResponse);

        return app;
    }
}
