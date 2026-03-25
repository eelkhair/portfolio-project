using Gateway.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Gateway.Tests.Unit.Middleware;

[Trait("Category", "Unit")]
public class RoutingMiddlewareTests
{
    private static IConfiguration BuildConfig(string? monolithValue)
    {
        var data = new Dictionary<string, string?>();
        if (monolithValue is not null)
            data["FeatureFlags:Monolith"] = monolithValue;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    [Fact]
    public async Task InvokeAsync_MonolithFlagTrue_SetsXModeToMonolith()
    {
        // Arrange
        var config = BuildConfig("true");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/companies";
        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new RoutingMiddleware(next, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Request.Headers["x-mode"].ToString().ShouldBe("monolith");
    }

    [Fact]
    public async Task InvokeAsync_MonolithFlagFalse_SetsXModeToAdmin()
    {
        // Arrange
        var config = BuildConfig("false");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/companies";
        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new RoutingMiddleware(next, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Request.Headers["x-mode"].ToString().ShouldBe("admin");
    }

    [Fact]
    public async Task InvokeAsync_MonolithFlagMissing_SetsXModeToAdmin()
    {
        // Arrange
        var config = BuildConfig(null);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/companies";
        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new RoutingMiddleware(next, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Request.Headers["x-mode"].ToString().ShouldBe("admin");
    }

    [Theory]
    [InlineData("/ai/v2/chat")]
    [InlineData("/ai/v2/some/nested/path")]
    [InlineData("/dapr/config")]
    [InlineData("/dapr/subscribe")]
    public async Task InvokeAsync_BypassPath_DoesNotSetXModeHeader(string path)
    {
        // Arrange
        var config = BuildConfig("true");
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new RoutingMiddleware(next, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Request.Headers.ContainsKey("x-mode").ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_BypassPath_StillCallsNext()
    {
        // Arrange
        var config = BuildConfig("true");
        var context = new DefaultHttpContext();
        context.Request.Path = "/ai/v2/chat";
        var nextCalled = false;
        var next = new RequestDelegate(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var middleware = new RoutingMiddleware(next, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_NonBypassPath_CallsNext()
    {
        // Arrange
        var config = BuildConfig("true");
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/jobs";
        var nextCalled = false;
        var next = new RequestDelegate(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var middleware = new RoutingMiddleware(next, config);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }
}
