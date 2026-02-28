using System.Diagnostics;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Sagas;
using JobBoard.IntegrationEvents.Company;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace connector_api.tests.Unit.Sagas;

[Trait("Category", "Unit")]
public class CompanyProvisioningSagaTests
{
    private readonly IMonolithClient _monolith = Substitute.For<IMonolithClient>();
    private readonly ICompanyApiClient _companyApi = Substitute.For<ICompanyApiClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly IUserApiClient _userApi = Substitute.For<IUserApiClient>();
    private readonly ILogger<CompanyProvisioningSaga> _logger = Substitute.For<ILogger<CompanyProvisioningSaga>>();
    private readonly ActivitySource _activitySource = new("test");
    private readonly CompanyProvisioningSaga _sut;

    private readonly CompanyCreatedV1Event _eventData;
    private readonly EventDto<CompanyCreatedV1Event> _event;
    private readonly CompanyCreateCompanyResult _companyResult;
    private readonly CompanyCreateUserResult _adminResult;
    private readonly CompanyCreatedUserApiPayload _userApiResponse;

    public CompanyProvisioningSagaTests()
    {
        _sut = new CompanyProvisioningSaga(
            _monolith, _companyApi, _jobApi, _userApi, _logger, _activitySource);

        _eventData = new CompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            IndustryUId: Guid.NewGuid(),
            AdminUId: Guid.NewGuid(),
            UserCompanyUId: Guid.NewGuid())
        {
            UserId = "auth0|user-456"
        };

        _event = new EventDto<CompanyCreatedV1Event>(
            _eventData.UserId, Guid.NewGuid().ToString(), _eventData);

        _companyResult = new CompanyCreateCompanyResult
        {
            Name = "Test Company",
            Email = "test@company.com",
            Website = "https://test.com",
            IndustryUId = Guid.NewGuid()
        };

        _adminResult = new CompanyCreateUserResult
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Smith",
            Email = "john@company.com"
        };

        _userApiResponse = new CompanyCreatedUserApiPayload
        {
            Auth0UserId = "auth0|org-user",
            Auth0OrganizationId = "org_abc123",
            CompanyName = "Test Company",
            FirstName = "John",
            LastName = "Smith",
            Email = "john@company.com",
            CompanyUId = _eventData.CompanyUId
        };

        _monolith.GetCompanyAndAdminForCreatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((_companyResult, _adminResult));

        _userApi.SendCompanyCreatedAsync(
                Arg.Any<EventDto<CompanyCreatedUserApiPayload>>(), Arg.Any<CancellationToken>())
            .Returns(_userApiResponse);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallGetCompanyAndAdmin()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _monolith.Received(1).GetCompanyAndAdminForCreatedEventAsync(
            _eventData.CompanyUId,
            _eventData.AdminUId,
            _eventData.UserId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSendToAllThreeServices()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _companyApi.Received(1).SendCompanyCreatedAsync(
            Arg.Any<CompanyCreatedCompanyApiPayload>(), Arg.Any<CancellationToken>());
        await _jobApi.Received(1).SendCompanyCreatedAsync(
            Arg.Any<EventDto<CompanyCreatedJobApiPayload>>(), Arg.Any<CancellationToken>());
        await _userApi.Received(1).SendCompanyCreatedAsync(
            Arg.Any<EventDto<CompanyCreatedUserApiPayload>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCallActivateWithUserApiResult()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _monolith.Received(1).ActivateCompanyAsync(
            Arg.Any<CompanyCreatedV1Event>(),
            Arg.Any<CompanyCreateCompanyResult>(),
            _userApiResponse,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldPassEventDataToActivate()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _monolith.Received(1).ActivateCompanyAsync(
            _eventData,
            Arg.Any<CompanyCreateCompanyResult>(),
            Arg.Any<CompanyCreatedUserApiPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCompanyResultToActivate()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _monolith.Received(1).ActivateCompanyAsync(
            Arg.Any<CompanyCreatedV1Event>(),
            _companyResult,
            Arg.Any<CompanyCreatedUserApiPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompanyPayload_ShouldHaveCorrectFields()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _companyApi.Received(1).SendCompanyCreatedAsync(
            Arg.Is<CompanyCreatedCompanyApiPayload>(p =>
                p.CompanyId == _eventData.CompanyUId &&
                p.Name == _companyResult.Name &&
                p.CompanyEmail == _companyResult.Email &&
                p.IndustryUId == _companyResult.IndustryUId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_JobPayload_ShouldHaveCorrectFields()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _jobApi.Received(1).SendCompanyCreatedAsync(
            Arg.Is<EventDto<CompanyCreatedJobApiPayload>>(p =>
                p.Data.UId == _eventData.CompanyUId &&
                p.Data.Name == _companyResult.Name &&
                p.UserId == _eventData.UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserPayload_ShouldHaveCorrectFields()
    {
        await _sut.HandleAsync(_event, CancellationToken.None);

        await _userApi.Received(1).SendCompanyCreatedAsync(
            Arg.Is<EventDto<CompanyCreatedUserApiPayload>>(p =>
                p.Data.FirstName == _adminResult.FirstName &&
                p.Data.LastName == _adminResult.LastName &&
                p.Data.Email == _adminResult.Email &&
                p.Data.CompanyUId == _eventData.CompanyUId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MonolithGetFails_ShouldPropagateException()
    {
        _monolith.GetCompanyAndAdminForCreatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Monolith unreachable"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));

        await _companyApi.DidNotReceive().SendCompanyCreatedAsync(
            Arg.Any<CompanyCreatedCompanyApiPayload>(), Arg.Any<CancellationToken>());
        await _jobApi.DidNotReceive().SendCompanyCreatedAsync(
            Arg.Any<EventDto<CompanyCreatedJobApiPayload>>(), Arg.Any<CancellationToken>());
        await _userApi.DidNotReceive().SendCompanyCreatedAsync(
            Arg.Any<EventDto<CompanyCreatedUserApiPayload>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FanOutServiceFails_ShouldPropagateException()
    {
        _companyApi.SendCompanyCreatedAsync(
                Arg.Any<CompanyCreatedCompanyApiPayload>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Company API down"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));

        await _monolith.DidNotReceive().ActivateCompanyAsync(
            Arg.Any<CompanyCreatedV1Event>(),
            Arg.Any<CompanyCreateCompanyResult>(),
            Arg.Any<CompanyCreatedUserApiPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserApiFails_ShouldPropagateException()
    {
        _userApi.SendCompanyCreatedAsync(
                Arg.Any<EventDto<CompanyCreatedUserApiPayload>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("User API down"));

        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));

        await _monolith.DidNotReceive().ActivateCompanyAsync(
            Arg.Any<CompanyCreatedV1Event>(),
            Arg.Any<CompanyCreateCompanyResult>(),
            Arg.Any<CompanyCreatedUserApiPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivateCompanyFails_ShouldPropagateException()
    {
        _monolith.ActivateCompanyAsync(
                Arg.Any<CompanyCreatedV1Event>(),
                Arg.Any<CompanyCreateCompanyResult>(),
                Arg.Any<CompanyCreatedUserApiPayload>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Activation failed"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.HandleAsync(_event, CancellationToken.None));
    }
}
