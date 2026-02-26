using System.Diagnostics;
using JobBoard.API.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Presentation;

[Trait("Category", "Unit")]
public class TraceIdMiddlewareTests : IDisposable
{
    private readonly ActivityListener _listener;

    public TraceIdMiddlewareTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRegisterOnStartingCallback()
    {
        // Use a feature that captures OnStarting callbacks
        var context = new DefaultHttpContext();
        var feature = new TestResponseFeature();
        context.Features.Set<IHttpResponseFeature>(feature);

        var middleware = new TraceIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        // The middleware registers an OnStarting callback
        feature.HasOnStartingRegistered.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        var nextCalled = false;
        var middleware = new TraceIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithNoExistingActivity_ShouldCreateOne()
    {
        // Ensure no current activity
        Activity.Current = null;

        string? capturedTraceId = null;
        var middleware = new TraceIdMiddleware(_ =>
        {
            // Inside the middleware pipeline, Activity.Current should be set
            capturedTraceId = Activity.Current?.TraceId.ToString();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        capturedTraceId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithExistingActivity_ShouldReuseIt()
    {
        using var source = new ActivitySource("TestMiddleware");
        using var activity = source.StartActivity("ExistingActivity");
        activity.ShouldNotBeNull();
        var expectedTraceId = activity.TraceId.ToString();

        string? capturedTraceId = null;
        var middleware = new TraceIdMiddleware(_ =>
        {
            capturedTraceId = Activity.Current?.TraceId.ToString();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        capturedTraceId.ShouldBe(expectedTraceId);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ShouldPropagateException()
    {
        var middleware = new TraceIdMiddleware(_ => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(new DefaultHttpContext()));
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    /// <summary>
    /// Test double that tracks OnStarting registration.
    /// </summary>
    private class TestResponseFeature : IHttpResponseFeature
    {
        public bool HasOnStartingRegistered { get; private set; }
        public Action? OnStartingCallback { get; set; }

        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => false;

        public void OnStarting(Func<object, Task> callback, object state)
        {
            HasOnStartingRegistered = true;
            OnStartingCallback?.Invoke();
        }

        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}
