using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.JobCreated;
using ConnectorAPI.Sagas;
using Dapr.Client;
using JobBoard.IntegrationEvents.Job;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Sagas;

[Trait("Category", "Unit")]
public class JobProvisioningSagaTests
{
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly ILogger<JobProvisioningSaga> _logger = Substitute.For<ILogger<JobProvisioningSaga>>();
    private readonly ActivitySource _activitySource = new("test");
    private readonly JobProvisioningSaga _sut;

    private readonly JobCreatedV1Event _eventData;
    private readonly EventDto<JobCreatedV1Event> _event;
    private readonly JobApiResponse _jobApiResponse;

    public JobProvisioningSagaTests()
    {
        _sut = new JobProvisioningSaga(_jobApi, _daprClient, _logger, _activitySource);

        _eventData = new JobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Software Engineer",
            AboutRole: "Build things",
            Location: "Remote",
            SalaryRange: "$100k-$150k",
            DraftId: Guid.NewGuid().ToString(),
            DeleteDraft: true,
            Responsibilities: ["Code", "Review"],
            Qualifications: ["C#", ".NET"],
            JobType: "FullTime")
        {
            UserId = "user-789"
        };

        _event = new EventDto<JobCreatedV1Event>(
            _eventData.UserId, Guid.NewGuid().ToString(), _eventData);

        _jobApiResponse = new JobApiResponse
        {
            UId = Guid.NewGuid(),
            Title = "Software Engineer",
            CompanyUId = _eventData.CompanyUId,
            Location = "Remote",
            JobType = "FullTime",
            AboutRole = "Build things"
        };

        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(), Arg.Any<CancellationToken>())
            .Returns(_jobApiResponse);
    }

    [Fact]
    public async Task HandleAsync_Success_ForwardsJobAndPublishes()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _jobApi.Received(1).SendJobCreatedAsync(
            Arg.Is<EventDto<JobCreatedJobApiPayload>>(p =>
                p.Data.Title == _eventData.Title &&
                p.Data.CompanyUId == _eventData.CompanyUId &&
                p.Data.Location == _eventData.Location &&
                p.UserId == _eventData.UserId),
            Arg.Any<CancellationToken>());

        await _daprClient.Received(1).PublishEventAsync(
            "rabbitmq.pubsub",
            "job.published.v2",
            Arg.Is<EventDto<JobApiResponse>>(p =>
                p.Data.UId == _eventData.UId &&
                p.Data.DraftId == _eventData.DraftId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_JobApiFails_Throws()
    {
        _jobApi.SendJobCreatedAsync(
                Arg.Any<EventDto<JobCreatedJobApiPayload>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API down"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));

        await _daprClient.DidNotReceive().PublishEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EventDto<JobApiResponse>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PublishFails_Throws()
    {
        _daprClient.PublishEventAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<EventDto<JobApiResponse>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Dapr unavailable"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));
    }
}
