using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Job;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.JobCreated;
using ConnectorAPI.Sagas;
using Dapr.Client;
using JobBoard.IntegrationEvents.Job;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Endpoints;

[Trait("Category", "Unit")]
public class JobCreatedEndpointTests : IAsyncDisposable
{
    private const string IdempotencyPrefix = "Provisioned:";

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly ActivitySource _activitySource = new("test.job-created");

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    private readonly JobCreatedV1Event _eventData;
    private readonly EventDto<JobCreatedV1Event> _event;

    public JobCreatedEndpointTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_daprClient);
        builder.Services.AddSingleton(_activitySource);
        builder.Services.AddSingleton(_jobApi);
        builder.Services.AddSingleton(Substitute.For<ILogger<JobProvisioningSaga>>());
        builder.Services.AddScoped<JobProvisioningSaga>();

        _app = builder.Build();
        _app.MapJobCreatedEndpoint();
        _app.StartAsync().GetAwaiter().GetResult();

        _client = _app.GetTestClient();

        _eventData = new JobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Software Engineer",
            AboutRole: "Build distributed systems",
            Location: "Remote",
            SalaryRange: "$120k-$180k",
            DraftId: Guid.NewGuid().ToString(),
            DeleteDraft: true,
            Responsibilities: ["Design APIs", "Write tests"],
            Qualifications: ["3+ years .NET", "Distributed systems"],
            JobType: "FullTime")
        {
            UserId = "keycloak|user-789"
        };

        _event = new EventDto<JobCreatedV1Event>(
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
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
                Arg.Any<CancellationToken>())
            .Returns(new JobApiResponse());

        var response = await _client.PostAsJsonAsync("/connector/job", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _jobApi.Received(1).SendJobCreatedAsync(
            Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
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

        var response = await _client.PostAsJsonAsync("/connector/job", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _jobApi.DidNotReceive().SendJobCreatedAsync(
            Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SavePendingState_BeforeCallingSaga()
    {
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
                Arg.Any<CancellationToken>())
            .Returns(new JobApiResponse());

        var response = await _client.PostAsJsonAsync("/connector/job", _event);

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
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
                Arg.Any<CancellationToken>())
            .Returns(new JobApiResponse());

        var response = await _client.PostAsJsonAsync("/connector/job", _event);

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
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        var response = await _client.PostAsJsonAsync("/connector/job", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Should_NotSaveDoneState_WhenSagaThrows()
    {
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        await _client.PostAsJsonAsync("/connector/job", _event);

        await _daprClient.DidNotReceive().SaveStateAsync(
            StateStores.Redis,
            Arg.Any<string>(),
            "done",
            metadata: Arg.Any<Dictionary<string, string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PublishEvent_AfterJobForwarded()
    {
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(),
                Arg.Any<CancellationToken>())
            .Returns(new JobApiResponse());

        var response = await _client.PostAsJsonAsync("/connector/job", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _daprClient.Received(1).PublishEventAsync(
            "rabbitmq.pubsub",
            "job.published.v2",
            Arg.Any<EventDto<JobApiResponse>>(),
            Arg.Any<CancellationToken>());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _activitySource.Dispose();
    }
}
