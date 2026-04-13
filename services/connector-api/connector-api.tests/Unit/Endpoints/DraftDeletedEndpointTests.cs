using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Draft;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Sagas;
using Dapr.Client;
using JobBoard.IntegrationEvents.Draft;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Endpoints;

[Trait("Category", "Unit")]
public class DraftDeletedEndpointTests : IAsyncDisposable
{
    private const string IdempotencyPrefix = "DraftDeleted:";

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly ActivitySource _activitySource = new("test.draft-deleted");

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    private readonly DraftDeletedV1Event _eventData;
    private readonly EventDto<DraftDeletedV1Event> _event;

    public DraftDeletedEndpointTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_daprClient);
        builder.Services.AddSingleton(_activitySource);
        builder.Services.AddSingleton(_jobApi);
        builder.Services.AddSingleton(Substitute.For<ILogger<DraftSyncSaga>>());
        builder.Services.AddScoped<DraftSyncSaga>();

        _app = builder.Build();
        _app.MapDraftDeletedEndpoint();
        _app.StartAsync().GetAwaiter().GetResult();

        _client = _app.GetTestClient();

        _eventData = new DraftDeletedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid())
        {
            UserId = "keycloak|user-789"
        };

        _event = new EventDto<DraftDeletedV1Event>(
            _eventData.UserId, Guid.NewGuid().ToString(), _eventData);

        // Default: state does not exist (not idempotent)
        _daprClient.GetStateAsync<string>(
                StateStores.Redis,
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((string?)null);
    }

    [Fact]
    public async Task Should_CallSaga_WhenNotIdempotent()
    {
        var response = await _client.PostAsJsonAsync("/connector/draft-deleted", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _jobApi.Received(1).DeleteDraftAsync(
            _eventData.UId,
            _eventData.UserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SkipProcessing_WhenIdempotent()
    {
        _daprClient.GetStateAsync<string>(
                StateStores.Redis,
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns("done");

        var response = await _client.PostAsJsonAsync("/connector/draft-deleted", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _jobApi.DidNotReceive().DeleteDraftAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SavePendingState_BeforeCallingSaga()
    {
        var response = await _client.PostAsJsonAsync("/connector/draft-deleted", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _daprClient.Received(1).SaveStateAsync(
            StateStores.Redis,
            Arg.Is<string>(k => k.StartsWith(IdempotencyPrefix)),
            "processing",
            metadata: Arg.Any<Dictionary<string, string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SaveDoneState_AfterSuccessfulSaga()
    {
        var response = await _client.PostAsJsonAsync("/connector/draft-deleted", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _daprClient.Received().SaveStateAsync(
            StateStores.Redis,
            Arg.Is<string>(k => k.StartsWith(IdempotencyPrefix)),
            "done",
            metadata: Arg.Any<Dictionary<string, string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnAccepted_EvenWhenSagaThrows()
    {
        _jobApi.DeleteDraftAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        var response = await _client.PostAsJsonAsync("/connector/draft-deleted", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Should_NotSaveDoneState_WhenSagaThrows()
    {
        _jobApi.DeleteDraftAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        await _client.PostAsJsonAsync("/connector/draft-deleted", _event);

        await _daprClient.DidNotReceive().SaveStateAsync(
            StateStores.Redis,
            Arg.Any<string>(),
            "done",
            metadata: Arg.Any<Dictionary<string, string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _activitySource.Dispose();
    }
}
