using System.Globalization;
using System.Threading.RateLimiting;
using HealthChecks.UI.Client;
using JobBoard.API.Infrastructure.SignalR;
using JobBoard.Domain;
using JobBoard.HealthChecks;
using JobBoard.Mcp.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace JobBoard.API.Infrastructure.Authorization;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---------------------------------------------------------------------
        // CORS
        // ---------------------------------------------------------------------
        services.AddCors(options =>
        {
            options.AddPolicy("AllowMyFrontendApp", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:4200",
                        "http://localhost:3000",
                        "http://localhost:5280",
                        "https://localhost:5280",
                        "http://127.0.0.1:4200",

                        "https://job-admin-dev.eelkhair.net",
                        "https://jobs-dev.eelkhair.net",
                        "https://jobs-dev.elkhair.tech",
                        "https://job-admin-dev.elkhair.tech",
                        "https://job-dev.eelkhair.net",
                        "http://192.168.1.200:9000",
                        "https://swagger-dev.eelkhair.net",

                        "http://192.168.1.112:9000",
                        "https://swagger.eelkhair.net",
                        "https://job-admin.eelkhair.net",
                        "https://job.eelkhair.net",
                        "https://jobs.eelkhair.net",
                        "http://127.0.0.1:5280",

                        "https://job-admin.elkhair.tech",
                        "https://jobs.elkhair.tech",
                        "https://job.elkhair.tech",

                        // Cloudflare Pages landing (cold-standby failover origin).
                        "https://landing-backup.elkhair.tech",
                        "https://elkhair.tech",
                        "https://www.elkhair.tech",
                        "https://dev.elkhair.tech",

                        // Keycloak origins — the login page's signup-link.js POSTs
                        // cross-origin to /api/Account/signup/*/anonymous for guest login.
                        "https://auth.eelkhair.net",
                        "https://auth.elkhair.tech",
                        "http://localhost:9999",
                        "https://localhost:9999"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("x-trace-id")
                    .AllowCredentials();
            });
        });

        // ---------------------------------------------------------------------
        // Rate limiting — "anonymous-signup" policy protects the guest signup
        // endpoints that create real Keycloak users without captcha. Partitioned
        // by client IP (X-Forwarded-For first hop, falling back to the direct
        // connection IP). Two layers per IP:
        //   • 10 requests / 1-hour fixed window   (burst cap)
        //   • 50 requests / 24-hour sliding window (daily cap, 6 × 4h segments)
        // Both layers must admit the request; either rejection → 429.
        // Shared budget across the public and admin anonymous endpoints.
        // ---------------------------------------------------------------------
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (ctx, _) =>
            {
                ctx.HttpContext.Response.Headers["Retry-After"] =
                    TimeSpan.FromHours(1).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            options.AddPolicy("anonymous-signup", httpContext =>
            {
                var partitionKey = ResolveClientIp(httpContext) ?? "unknown";
                return RateLimitPartition.Get(partitionKey, _ => new AnonymousSignupLimiter());
            });
        });

        // ---------------------------------------------------------------------
        // JWT Bearer Auth (Keycloak) + InternalApiKey
        // ---------------------------------------------------------------------
        services
            .AddKeycloakJwtAuth(configuration, jwt =>
            {
                // Accept tokens issued by either domain (eelkhair.net or elkhair.tech)
                var authority = configuration["Keycloak:Authority"] ?? string.Empty;
                var realmPath = authority.Contains("/realms/")
                    ? authority[authority.IndexOf("/realms/", StringComparison.Ordinal)..]
                    : "/realms/job-board";
                jwt.TokenValidationParameters.ValidIssuers =
                [
                    $"https://auth.eelkhair.net{realmPath}",
                    $"https://auth.elkhair.tech{realmPath}"
                ];

                // SignalR: read access_token from query string for WebSocket connections
                jwt.Events!.OnMessageReceived = context =>
                {
                    var path = context.HttpContext.Request.Path;
                    var token = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(token) &&
                        path.StartsWithSegments("/hubs/notifications", StringComparison.Ordinal))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, InternalApiKeyAuthenticationHandler>(
                "InternalApiKey", _ => { });

        // ---------------------------------------------------------------------
        // Authorization Policies
        // ---------------------------------------------------------------------
        services
            .AddAuthorizationBuilder()

            // Group-based policies (Keycloak groups)
            .AddPolicy(AuthorizationPolicies.Admin, policy =>
                policy.RequireClaim("groups", UserRoles.Admin))

            .AddPolicy(AuthorizationPolicies.Recruiter, policy =>
                policy.RequireClaim("groups", UserRoles.Recruiter))

            .AddPolicy(AuthorizationPolicies.AllUsers, policy =>
                policy.RequireClaim("groups", UserRoles.Admin, UserRoles.Recruiter))

            .AddPolicy(AuthorizationPolicies.Dashboard, policy =>
                policy.RequireClaim("groups", UserRoles.Admin, UserRoles.CompanyAdmin))

            .AddPolicy(AuthorizationPolicies.InternalOrJwt, policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "InternalApiKey")
                    .RequireAuthenticatedUser());

        return services;
    }

    // -------------------------------------------------------------------------
    // Application Middleware Pipeline
    // -------------------------------------------------------------------------
    public static WebApplication UseApplicationServices(this WebApplication app)
    {

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseStaticFiles();
        app.UseODataRouteDebug();
        app.UseRouting();
        app.UseCors("AllowMyFrontendApp");
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    private static string? ResolveClientIp(HttpContext httpContext)
    {
        // YARP gateway and Cloudflare both set X-Forwarded-For. Take the first (leftmost) hop.
        var forwarded = httpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first)) return first;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    // -------------------------------------------------------------------------
    // Two-layer rate limiter: fixed 1h burst cap + rolling 24h daily cap.
    // A request is admitted only if both underlying limiters admit it. If the
    // hour accepts but the day rejects, the hour permit is already consumed —
    // acceptable trade-off since the IP is already over its daily budget.
    // -------------------------------------------------------------------------
    private sealed class AnonymousSignupLimiter : RateLimiter
    {
        private readonly FixedWindowRateLimiter _hour;
        private readonly SlidingWindowRateLimiter _day;

        public AnonymousSignupLimiter()
        {
            _hour = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
            _day = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromHours(24),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
        }

        public override TimeSpan? IdleDuration
        {
            get
            {
                var h = _hour.IdleDuration;
                var d = _day.IdleDuration;
                if (h is null || d is null) return null;
                return h.Value < d.Value ? h : d;
            }
        }

        public override RateLimiterStatistics? GetStatistics()
        {
            var h = _hour.GetStatistics();
            var d = _day.GetStatistics();
            if (h is null) return d;
            if (d is null) return h;
            return h.CurrentAvailablePermits < d.CurrentAvailablePermits ? h : d;
        }

        protected override RateLimitLease AttemptAcquireCore(int permitCount)
        {
            var hourLease = _hour.AttemptAcquire(permitCount);
            if (!hourLease.IsAcquired) return hourLease;

            var dayLease = _day.AttemptAcquire(permitCount);
            if (!dayLease.IsAcquired)
            {
                hourLease.Dispose();
                return dayLease;
            }

            return new CompositeLease(hourLease, dayLease);
        }

        protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
        {
            // QueueLimit = 0 on both inner limiters → no actual async waiting; synchronous attempt is equivalent.
            return ValueTask.FromResult(AttemptAcquireCore(permitCount));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hour.Dispose();
                _day.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class CompositeLease(RateLimitLease a, RateLimitLease b) : RateLimitLease
        {
            private bool _disposed;

            public override bool IsAcquired => a.IsAcquired && b.IsAcquired;

            public override IEnumerable<string> MetadataNames =>
                a.MetadataNames.Concat(b.MetadataNames);

            public override bool TryGetMetadata(string metadataName, out object? metadata)
            {
                if (a.TryGetMetadata(metadataName, out metadata)) return true;
                return b.TryGetMetadata(metadataName, out metadata);
            }

            protected override void Dispose(bool disposing)
            {
                if (_disposed) return;
                _disposed = true;
                if (disposing)
                {
                    a.Dispose();
                    b.Dispose();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Graceful Shutdown for OTEL Providers
    // -------------------------------------------------------------------------
    public static void Start(this WebApplication app)
    {
        var tracerProvider = app.Services.GetService<TracerProvider>();
        var loggerProvider = app.Services.GetService<LoggerProvider>();

        try
        {
            app.MapCustomHealthChecks("/healthzEndpoint", "/liveness", UIResponseWriter.WriteHealthCheckUIResponse);
            app.MapGet("/", (HttpContext ctx) => ctx.Response.Redirect("/swagger")).ExcludeFromDescription();
            app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();
            app.Run();
        }
        finally
        {
            tracerProvider?.Shutdown();
            loggerProvider?.Shutdown();
        }
    }
}
