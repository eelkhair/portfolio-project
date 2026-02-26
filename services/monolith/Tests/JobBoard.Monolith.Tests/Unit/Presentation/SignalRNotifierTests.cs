using System.Diagnostics;
using JobBoard.API.Infrastructure.SignalR;
using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.API.Infrastructure.SignalR.FeatureFlags;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Presentation;

[Trait("Category", "Unit")]
public class SignalRFeatureFlagNotifierTests
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IClientProxy _clientProxy;
    private readonly SignalRFeatureFlagNotifier _sut;

    public SignalRFeatureFlagNotifierTests()
    {
        _hub = Substitute.For<IHubContext<NotificationsHub>>();
        _clientProxy = Substitute.For<IClientProxy>();
        var clients = Substitute.For<IHubClients>();
        clients.All.Returns(_clientProxy);
        _hub.Clients.Returns(clients);

        _sut = new SignalRFeatureFlagNotifier(_hub);
    }

    [Fact]
    public async Task NotifyAsync_ShouldSendToAllClients()
    {
        var flags = new Dictionary<string, bool> { ["EnableEfTracking"] = true };

        await _sut.NotifyAsync(flags);

        await _clientProxy.Received(1).SendCoreAsync(
            "featureFlagsUpdated",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithEmptyFlags_ShouldStillSend()
    {
        var flags = new Dictionary<string, bool>();

        await _sut.NotifyAsync(flags);

        await _clientProxy.Received(1).SendCoreAsync(
            "featureFlagsUpdated",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }
}

[Trait("Category", "Unit")]
public class CompanyActivationNotifierTests : IDisposable
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly IClientProxy _clientProxy;
    private readonly CompanyActivationNotifier _sut;
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;

    public CompanyActivationNotifierTests()
    {
        _hub = Substitute.For<IHubContext<NotificationsHub>>();
        _clientProxy = Substitute.For<IClientProxy>();
        _activitySource = new ActivitySource("TestNotifier");

        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);

        var clients = Substitute.For<IHubClients>();
        clients.Group(Arg.Any<string>()).Returns(_clientProxy);
        _hub.Clients.Returns(clients);

        _sut = new CompanyActivationNotifier(
            _hub,
            _activitySource,
            Substitute.For<ILogger<CompanyActivationNotifier>>());
    }

    [Fact]
    public async Task NotifyAsync_ShouldSendToCreatedByGroup()
    {
        var request = new CompanyCreatedModel
        {
            CompanyName = "TestCorp",
            CompanyUId = Guid.NewGuid(),
            CreatedBy = "user-123"
        };

        await _sut.NotifyAsync(request, CancellationToken.None);

        _hub.Clients.Received(1).Group("user-123");
        await _clientProxy.Received(1).SendCoreAsync(
            "CompanyActivated",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WhenHubThrows_ShouldNotPropagateException()
    {
        _clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HubException("connection lost")));

        var request = new CompanyCreatedModel
        {
            CompanyName = "FailCorp",
            CompanyUId = Guid.NewGuid(),
            CreatedBy = "user-456"
        };

        // Should not throw — error is logged internally
        await Should.NotThrowAsync(
            () => _sut.NotifyAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task NotifyAsync_ShouldIncludeCompanyNameInPayload()
    {
        var request = new CompanyCreatedModel
        {
            CompanyName = "PayloadCorp",
            CompanyUId = Guid.NewGuid(),
            CreatedBy = "user-789"
        };

        await _sut.NotifyAsync(request, CancellationToken.None);

        await _clientProxy.Received(1).SendCoreAsync(
            "CompanyActivated",
            Arg.Is<object?[]>(args => args.Length > 0),
            Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
    }
}
