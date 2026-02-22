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

    public static IReadOnlyList<ClusterConfig> GetClusters(bool useDapr)
    {
        Log.LogInformation("Building YARP clusters — UseDapr: {UseDapr}", useDapr);
        return new[]
        {
            Cluster("ai-v2", "ai-service-v2", useDapr),
            Cluster("admin", "admin-api", useDapr),
            Cluster("monolith", "monolith-api", useDapr),
        };
    }

    private static ClusterConfig Cluster(string clusterId, string serviceName, bool useDapr)
    {
        var address = useDapr
            ? $"http://localhost:3500/v1.0/invoke/{serviceName}/method/"
            : $"http://{serviceName}:8080/";

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