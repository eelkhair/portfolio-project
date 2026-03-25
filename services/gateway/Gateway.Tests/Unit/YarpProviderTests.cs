using Gateway.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Gateway.Tests.Unit;

[Trait("Category", "Unit")]
public class YarpProviderTests
{
    [Fact]
    public void GetRoutes_ReturnsExpectedNumberOfRoutes()
    {
        // Act
        var routes = YarpProvider.GetRoutes();

        // Assert
        routes.Count.ShouldBe(4);
    }

    [Fact]
    public void GetRoutes_ContainsAiV2Route()
    {
        // Act
        var routes = YarpProvider.GetRoutes();
        var aiRoute = routes.SingleOrDefault(r => r.RouteId == "ai-v2");

        // Assert
        aiRoute.ShouldNotBeNull();
        aiRoute.ClusterId.ShouldBe("ai-v2");
        aiRoute.Match.Path.ShouldBe("/ai/v2/{**catch-all}");
    }

    [Fact]
    public void GetRoutes_AiV2Route_HasPathRemovePrefixTransform()
    {
        // Act
        var routes = YarpProvider.GetRoutes();
        var aiRoute = routes.Single(r => r.RouteId == "ai-v2");

        // Assert
        aiRoute.Transforms.ShouldNotBeNull();
        aiRoute.Transforms!.Count.ShouldBe(1);
        aiRoute.Transforms!.First().ShouldContainKeyAndValue("PathRemovePrefix", "/ai/v2");
    }

    [Fact]
    public void GetRoutes_ContainsPublicApiRoute()
    {
        // Act
        var routes = YarpProvider.GetRoutes();
        var publicRoute = routes.SingleOrDefault(r => r.RouteId == "public-api");

        // Assert
        publicRoute.ShouldNotBeNull();
        publicRoute.ClusterId.ShouldBe("monolith");
        publicRoute.Match.Path.ShouldBe("/api/public/{**catch-all}");
        publicRoute.Order.ShouldBe(0);
    }

    [Fact]
    public void GetRoutes_ContainsAdminApiRoute_WithXModeHeaderMatch()
    {
        // Act
        var routes = YarpProvider.GetRoutes();
        var adminRoute = routes.SingleOrDefault(r => r.RouteId == "admin-api");

        // Assert
        adminRoute.ShouldNotBeNull();
        adminRoute.ClusterId.ShouldBe("admin");
        adminRoute.Match.Path.ShouldBe("/{**catch-all}");
        adminRoute.Match.Headers.ShouldNotBeNull();
        adminRoute.Match.Headers!.First().Name.ShouldBe("x-mode");
        adminRoute.Match.Headers!.First().Values!.ShouldContain("admin");
    }

    [Fact]
    public void GetRoutes_ContainsMonolithApiRoute_WithXModeHeaderMatch()
    {
        // Act
        var routes = YarpProvider.GetRoutes();
        var monolithRoute = routes.SingleOrDefault(r => r.RouteId == "monolith-api");

        // Assert
        monolithRoute.ShouldNotBeNull();
        monolithRoute.ClusterId.ShouldBe("monolith");
        monolithRoute.Match.Path.ShouldBe("/{**catch-all}");
        monolithRoute.Match.Headers.ShouldNotBeNull();
        monolithRoute.Match.Headers!.First().Name.ShouldBe("x-mode");
        monolithRoute.Match.Headers!.First().Values!.ShouldContain("monolith");
    }

    [Fact]
    public void GetClusters_ReturnsThreeClusters()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiServiceUrl"] = "http://localhost:5010",
                ["AdminApiUrl"] = "http://localhost:5020",
                ["MonolithUrl"] = "http://localhost:5030"
            })
            .Build();

        // Act
        var clusters = YarpProvider.GetClusters(config);

        // Assert
        clusters.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("ai-v2", "AiServiceUrl", "http://localhost:5010")]
    [InlineData("admin", "AdminApiUrl", "http://localhost:5020")]
    [InlineData("monolith", "MonolithUrl", "http://localhost:5030")]
    public void GetClusters_ClusterDestination_HasCorrectAddress(
        string clusterId, string configKey, string expectedAddress)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [configKey] = expectedAddress
            })
            .Build();

        // Act
        var clusters = YarpProvider.GetClusters(config);
        var cluster = clusters.SingleOrDefault(c => c.ClusterId == clusterId);

        // Assert
        cluster.ShouldNotBeNull();
        cluster.Destinations.ShouldNotBeNull();
        cluster.Destinations!["default"].Address.ShouldBe(expectedAddress);
    }

    [Fact]
    public void GetClusters_MissingConfig_UsesDefaultAddresses()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var clusters = YarpProvider.GetClusters(config);

        // Assert
        var aiCluster = clusters.Single(c => c.ClusterId == "ai-v2");
        aiCluster.Destinations!["default"].Address.ShouldBe("http://ai-service-v2:8080");

        var adminCluster = clusters.Single(c => c.ClusterId == "admin");
        adminCluster.Destinations!["default"].Address.ShouldBe("http://admin-api:8080");

        var monolithCluster = clusters.Single(c => c.ClusterId == "monolith");
        monolithCluster.Destinations!["default"].Address.ShouldBe("http://monolith-api:8080");
    }
}
