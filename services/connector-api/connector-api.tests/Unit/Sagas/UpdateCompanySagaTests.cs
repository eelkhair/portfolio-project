using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Sagas;
using JobBoard.IntegrationEvents.Company;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Sagas;

[Trait("Category", "Unit")]
public class UpdateCompanySagaTests
{
    private readonly IMonolithClient _monolith = Substitute.For<IMonolithClient>();
    private readonly ICompanyApiClient _companyApi = Substitute.For<ICompanyApiClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly ILogger<UpdateCompanySaga> _logger = Substitute.For<ILogger<UpdateCompanySaga>>();
    private readonly ActivitySource _activitySource = new("test");
    private readonly UpdateCompanySaga _sut;

    private readonly CompanyUpdatedV1Event _eventData;
    private readonly EventDto<CompanyUpdatedV1Event> _event;
    private readonly CompanyUpdateCompanyResult _companyResult;

    public UpdateCompanySagaTests()
    {
        _sut = new UpdateCompanySaga(
            _monolith, _companyApi, _jobApi, _logger, _activitySource);

        _eventData = new CompanyUpdatedV1Event(
            CompanyUId: Guid.NewGuid(),
            IndustryUId: Guid.NewGuid())
        {
            UserId = "user-123"
        };

        _event = new EventDto<CompanyUpdatedV1Event>(
            _eventData.UserId, Guid.NewGuid().ToString(), _eventData);

        _companyResult = new CompanyUpdateCompanyResult
        {
            Name = "Updated Corp",
            Email = "info@updated.com",
            Website = "https://updated.com",
            Phone = "+1234567890",
            Description = "A company",
            About = "About us",
            EEO = "Equal opportunity",
            Founded = new DateTime(2020, 1, 1),
            Size = "50-100",
            Logo = "https://logo.png",
            IndustryUId = _eventData.IndustryUId
        };

        _monolith.GetCompanyForUpdatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_companyResult);
    }

    [Fact]
    public async Task HandleAsync_Success_CallsFetchAndFanOut()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _monolith.Received(1).GetCompanyForUpdatedEventAsync(
            _eventData.CompanyUId,
            _eventData.UserId,
            Arg.Any<CancellationToken>());

        await _companyApi.Received(1).SendCompanyUpdatedAsync(
            _eventData.CompanyUId,
            Arg.Any<CompanyUpdatedCompanyApiPayload>(),
            Arg.Any<CancellationToken>());

        await _jobApi.Received(1).SendCompanyUpdatedAsync(
            _eventData.CompanyUId,
            Arg.Any<EventDto<CompanyUpdatedJobApiPayload>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FetchFails_Throws()
    {
        _monolith.GetCompanyForUpdatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Monolith unreachable"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));

        await _companyApi.DidNotReceive().SendCompanyUpdatedAsync(
            Arg.Any<Guid>(),
            Arg.Any<CompanyUpdatedCompanyApiPayload>(),
            Arg.Any<CancellationToken>());

        await _jobApi.DidNotReceive().SendCompanyUpdatedAsync(
            Arg.Any<Guid>(),
            Arg.Any<EventDto<CompanyUpdatedJobApiPayload>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FanOutFails_Throws()
    {
        _companyApi.SendCompanyUpdatedAsync(
                Arg.Any<Guid>(),
                Arg.Any<CompanyUpdatedCompanyApiPayload>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Company API down"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));
    }
}
