using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.Drafts;
using ConnectorAPI.Sagas;
using JobBoard.IntegrationEvents.Draft;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Sagas;

[Trait("Category", "Unit")]
public class DraftSyncSagaTests
{
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly ILogger<DraftSyncSaga> _logger = Substitute.For<ILogger<DraftSyncSaga>>();
    private readonly ActivitySource _activitySource = new("test");
    private readonly DraftSyncSaga _sut;

    public DraftSyncSagaTests()
    {
        _sut = new DraftSyncSaga(_jobApi, _logger, _activitySource);
    }

    [Fact]
    public async Task HandleSaveAsync_Success_CallsSaveDraft()
    {
        var draftUId = Guid.NewGuid();
        var companyUId = Guid.NewGuid();
        var eventData = new DraftSavedV1Event(
            UId: draftUId,
            CompanyUId: companyUId,
            Title: "Draft Job",
            AboutRole: "Do stuff",
            Location: "NYC",
            JobType: "FullTime",
            SalaryRange: "$80k",
            Notes: "Some notes",
            Responsibilities: ["Task A"],
            Qualifications: ["Skill B"])
        {
            UserId = "user-draft-1"
        };

        var @event = new EventDto<DraftSavedV1Event>(
            eventData.UserId, Guid.NewGuid().ToString(), eventData);

        _jobApi.SaveDraftAsync(
                Arg.Any<Guid>(), Arg.Any<EventDto<SaveDraftPayload>>(), Arg.Any<CancellationToken>())
            .Returns(new DraftResponse());

        await _sut.HandleSaveAsync(@event, CancellationToken.None);

        await _jobApi.Received(1).SaveDraftAsync(
            companyUId,
            Arg.Is<EventDto<SaveDraftPayload>>(p =>
                p.Data.Id == draftUId.ToString() &&
                p.Data.Title == "Draft Job" &&
                p.Data.AboutRole == "Do stuff" &&
                p.Data.Location == "NYC" &&
                p.Data.JobType == "FullTime" &&
                p.Data.SalaryRange == "$80k" &&
                p.Data.Notes == "Some notes" &&
                p.UserId == eventData.UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleSaveAsync_Fails_Throws()
    {
        var eventData = new DraftSavedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Draft",
            AboutRole: "Role",
            Location: "LA",
            JobType: "Contract",
            SalaryRange: null,
            Notes: "",
            Responsibilities: [],
            Qualifications: [])
        {
            UserId = "user-draft-2"
        };

        var @event = new EventDto<DraftSavedV1Event>(
            eventData.UserId, Guid.NewGuid().ToString(), eventData);

        _jobApi.SaveDraftAsync(
                Arg.Any<Guid>(), Arg.Any<EventDto<SaveDraftPayload>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API down"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleSaveAsync(@event, CancellationToken.None));
    }

    [Fact]
    public async Task HandleDeleteAsync_Success_CallsDeleteDraft()
    {
        var draftUId = Guid.NewGuid();
        var eventData = new DraftDeletedV1Event(
            UId: draftUId,
            CompanyUId: Guid.NewGuid())
        {
            UserId = "user-draft-3"
        };

        var @event = new EventDto<DraftDeletedV1Event>(
            eventData.UserId, Guid.NewGuid().ToString(), eventData);

        await _sut.HandleDeleteAsync(@event, CancellationToken.None);

        await _jobApi.Received(1).DeleteDraftAsync(
            draftUId,
            eventData.UserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleDeleteAsync_Fails_Throws()
    {
        var eventData = new DraftDeletedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid())
        {
            UserId = "user-draft-4"
        };

        var @event = new EventDto<DraftDeletedV1Event>(
            eventData.UserId, Guid.NewGuid().ToString(), eventData);

        _jobApi.DeleteDraftAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Job API unreachable"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleDeleteAsync(@event, CancellationToken.None));
    }
}
