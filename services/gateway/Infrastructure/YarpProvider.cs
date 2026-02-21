using Yarp.ReverseProxy.Configuration;

namespace Gateway.Api.Infrastructure;

public static class YarpProvider
{
    public static IReadOnlyList<RouteConfig> GetRoutes() => new[]
    {
        new RouteConfig
        {
            RouteId = "ai-v2",
            ClusterId = "ai-v2",
            Match = new RouteMatch
            {
                Path = "/ai/v2/{**catch-all}"
            }
        },
        new RouteConfig
        {
            RouteId = "admin-api",
            ClusterId = "admin",
            Match = new RouteMatch
            {
                Path = "/api/{**catch-all}",
                Headers = new[]
                {
                    new RouteHeader
                    {
                        Name = "x-mode",
                        Values = new[] { "admin" }
                    }
                }
            }, 
            Transforms = new[]
            {
                new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" },
            }

        },
        new RouteConfig
        {
            RouteId = "monolith-api",
            ClusterId = "monolith",
            Match = new RouteMatch
            {
                Path = "/api/{**catch-all}",
                Headers = new[]
                {
                    new RouteHeader
                    {
                        Name = "x-mode",
                        Values = new[] { "monolith" }
                    }
                }
            }, 
            Transforms = new[]
            {
                new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" },
            }
        }
    };
    public static IReadOnlyList<ClusterConfig> GetClusters() => new[]
    {
        DaprCluster("ai-v2", "ai-service-v2"),
        DaprCluster("admin", "admin-api"),
        DaprCluster("monolith", "monolith-api"),
    };

    private static ClusterConfig DaprCluster(string clusterId, string appId) => new()
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["dapr"] = new()
            {
                Address = $"http://localhost:3500/v1.0/invoke/{appId}/method/"
            }
        }
    };
}