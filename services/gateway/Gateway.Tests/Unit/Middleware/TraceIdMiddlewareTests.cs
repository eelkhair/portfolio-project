using Gateway.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Shouldly;

namespace Gateway.Tests.Unit.Middleware;

[Trait("Category", "Unit")]
public class TraceIdMiddlewareTests
{
    /// <summary>
    /// A response feature that captures OnStarting callbacks so we can invoke them in tests.
    /// </summary>
    private sealed class TestHttpResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = new();

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStartingCallbacks.Add((callback, state));
        }

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public async Task FireOnStartingAsync()
        {
            HasStarted = true;
            foreach (var (callback, state) in _onStartingCallbacks)
            {
                await callback(state);
            }
        }
    }

    [Fact]
    public async Task InvokeAsync_ResponseHasTraceIdHeader_SetsXTraceIdResponseHeader()
    {
        // Arrange
        var responseFeature = new TestHttpResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpResponseFeature>(responseFeature);
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        var context = new DefaultHttpContext(features);

        var next = new RequestDelegate(ctx =>
        {
            ctx.Response.Headers["trace-id"] = "abc123";
            return Task.CompletedTask;
        });
        var middleware = new TraceIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        context.Response.Headers["x-trace-id"].ToString().ShouldBe("abc123");
    }

    [Fact]
    public async Task InvokeAsync_NoTraceIdHeader_DoesNotAddXTraceIdResponseHeader()
    {
        // Arrange
        var responseFeature = new TestHttpResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpResponseFeature>(responseFeature);
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        var context = new DefaultHttpContext(features);

        var next = new RequestDelegate(_ => Task.CompletedTask);
        var middleware = new TraceIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        context.Response.Headers.ContainsKey("x-trace-id").ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_EmptyTraceIdHeader_DoesNotAddXTraceIdResponseHeader()
    {
        // Arrange
        var responseFeature = new TestHttpResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpResponseFeature>(responseFeature);
        features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        var context = new DefaultHttpContext(features);

        var next = new RequestDelegate(ctx =>
        {
            ctx.Response.Headers["trace-id"] = "";
            return Task.CompletedTask;
        });
        var middleware = new TraceIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);
        await responseFeature.FireOnStartingAsync();

        // Assert
        context.Response.Headers.ContainsKey("x-trace-id").ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Always_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var next = new RequestDelegate(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var middleware = new TraceIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.ShouldBeTrue();
    }
}
