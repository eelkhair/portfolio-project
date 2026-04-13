using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.Infrastructure;

public static class YarpProvider
{
    private static readonly ILoggerFactory LogFactory = LoggerFactory.Create(b => b.AddConsole());
    private static readonly ILogger Log = LogFactory.CreateLogger("YarpProvider");

    public static IReadOnlyList<RouteConfig> GetRoutes() => new[]
    {
        new RouteConfig
        {
            RouteId = "jaeger-api",
            ClusterId = "jaeger",
            Match = new RouteMatch
            {
                Path = "/jaeger-api/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathRemovePrefix"] = "/jaeger-api"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "ai-v2",
            ClusterId = "ai-v2",
            Match = new RouteMatch
            {
                Path = "/ai/v2/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathRemovePrefix"] = "/ai/v2"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "public-api",
            ClusterId = "monolith",
            Match = new RouteMatch
            {
                Path = "/api/public/{**catch-all}"
            },
            Order = 0 // Higher priority than catch-all routes
        },
        new RouteConfig
        {
            RouteId = "resumes-api",
            ClusterId = "monolith",
            Match = new RouteMatch
            {
                Path = "/api/resumes/{**catch-all}"
            },
            Order = 0
        },
        new RouteConfig
        {
            RouteId = "applicant-api",
            ClusterId = "monolith",
            Match = new RouteMatch
            {
                Path = "/api/applicant/{**catch-all}"
            },
            Order = 0
        },
        new RouteConfig
        {
            RouteId = "applications-api",
            ClusterId = "monolith",
            Match = new RouteMatch
            {
                Path = "/api/applications/{**catch-all}"
            },
            Order = 0
        },
        new RouteConfig
        {
            RouteId = "admin-api",
            ClusterId = "admin",
            Match = new RouteMatch
            {
                Path = "/{**catch-all}",
                Headers = new[]
                {
                    new RouteHeader
                    {
                        Name = "x-mode",
                        Values = new[] { "admin" }
                    }
                }
            }
        },
        new RouteConfig
        {
            RouteId = "monolith-api",
            ClusterId = "monolith",
            Match = new RouteMatch
            {
                Path = "/{**catch-all}",
                Headers = new[]
                {
                    new RouteHeader
                    {
                        Name = "x-mode",
                        Values = new[] { "monolith" }
                    }
                }
            }
        }
    };

    public static IReadOnlyList<ClusterConfig> GetClusters(IConfiguration config)
    {
        return new[]
        {
            Cluster("jaeger", config["JaegerApiUrl"] ?? "http://localhost:16686"),
            Cluster("ai-v2", config["AiServiceUrl"] ?? "http://ai-service-v2:8080"),
            Cluster("admin", config["AdminApiUrl"] ?? "http://admin-api:8080"),
            Cluster("monolith", config["MonolithUrl"] ?? "http://monolith-api:8080"),
        };
    }

    private static ClusterConfig Cluster(string clusterId, string address)
    {
        Log.LogInformation("Cluster {ClusterId} -> {Address}", clusterId, address);

        return new()
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["default"] = new() { Address = address }
            }
        };
    }
}