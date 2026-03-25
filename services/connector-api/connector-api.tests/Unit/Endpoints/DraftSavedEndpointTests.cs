using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Draft;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.Drafts;
using ConnectorAPI.Sagas;
using Dapr.Client;
using JobBoard.IntegrationEvents.Draft;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Endpoints;

[Trait("Category", "Unit")]
public class DraftSavedEndpointTests : IAsyncDisposable
{
    private const string IdempotencyPrefix = "DraftSaved:";

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly ActivitySource _activitySource = new("test.draft-saved");

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    private readonly DraftSavedV1Event _eventData;
    private readonly EventDto<DraftSavedV1Event> _event;

    public DraftSavedEndpointTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_daprClient);
        builder.Services.AddSingleton(_activitySource);
        builder.Services.AddSingleton(_jobApi);
        builder.Services.AddSingleton(Substitute.For<ILogger<DraftSyncSaga>>());
        builder.Services.AddScoped<DraftSyncSaga>();

        _app = builder.Build();
        _app.MapDraftSavedEndpoint();
        _app.StartAsync().GetAwaiter().GetResult();

        _client = _app.GetTestClient();

        _eventData = new DraftSavedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Senior Developer",
            AboutRole: "Build things",
            Location: "Remote",
            JobType: "FullTime",
            SalaryRange: "$100k-$150k",
            Notes: "Urgent hire",
            Responsibilities: ["Design", "Code"],
            Qualifications: ["5+ years experience"])
        {
            UserId = "keycloak|user-456"
        };

        _event = new EventDto<DraftSavedV1Event>(
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
        var response = await _client.PostAsJsonAsync("/connector/draft-saved", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _jobApi.Received(1).SaveDraftAsync(
            _eventData.CompanyUId,
            Arg.Any<EventDto<SaveDraftPayload>>(),
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

        var response = await _client.PostAsJsonAsync("/connector/draft-saved", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _jobApi.DidNotReceive().SaveDraftAsync(
            Arg.Any<Guid>(),
            Arg.Any<EventDto<SaveDraftPayload>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SavePendingState_BeforeCallingSaga()
    {
        var response = await _client.PostAsJsonAsync("/connector/draft-saved", _event);

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
        var response = await _client.PostAsJsonAsync("/connector/draft-saved", _event);

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
        _jobApi.SaveDraftAsync(
                Arg.Any<Guid>(),
                Arg.Any<EventDto<SaveDraftPayload>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        var response = await _client.PostAsJsonAsync("/connector/draft-saved", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Should_NotSaveDoneState_WhenSagaThrows()
    {
        _jobApi.SaveDraftAsync(
                Arg.Any<Guid>(),
                Arg.Any<EventDto<SaveDraftPayload>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        await _client.PostAsJsonAsync("/connector/draft-saved", _event);

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
