using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Company;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Sagas;
using Dapr.Client;
using JobBoard.IntegrationEvents.Company;
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
public class CompanyCreatedEndpointTests : IAsyncDisposable
{
    private const string IdempotencyPrefix = "Provisioned:";

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly IMonolithClient _monolith = Substitute.For<IMonolithClient>();
    private readonly ICompanyApiClient _companyApi = Substitute.For<ICompanyApiClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly IUserApiClient _userApi = Substitute.For<IUserApiClient>();
    private readonly ActivitySource _activitySource = new("test.company-created");

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    private readonly CompanyCreatedV1Event _eventData;
    private readonly EventDto<CompanyCreatedV1Event> _event;

    public CompanyCreatedEndpointTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_daprClient);
        builder.Services.AddSingleton(_activitySource);
        builder.Services.AddSingleton(_monolith);
        builder.Services.AddSingleton(_companyApi);
        builder.Services.AddSingleton(_jobApi);
        builder.Services.AddSingleton(_userApi);
        builder.Services.AddSingleton(Substitute.For<ILogger<CompanyProvisioningSaga>>());
        builder.Services.AddScoped<CompanyProvisioningSaga>();

        _app = builder.Build();
        _app.MapCompanyCreatedEndpoint();
        _app.StartAsync().GetAwaiter().GetResult();

        _client = _app.GetTestClient();

        _eventData = new CompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            IndustryUId: Guid.NewGuid(),
            AdminUId: Guid.NewGuid(),
            UserCompanyUId: Guid.NewGuid())
        {
            UserId = "keycloak|user-123"
        };

        _event = new EventDto<CompanyCreatedV1Event>(
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
        var response = await _client.PostAsJsonAsync("/connector/company", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _monolith.Received(1).GetCompanyAndAdminForCreatedEventAsync(
            _eventData.CompanyUId,
            _eventData.AdminUId,
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

        var response = await _client.PostAsJsonAsync("/connector/company", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _monolith.DidNotReceive().GetCompanyAndAdminForCreatedEventAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SavePendingState_BeforeCallingSaga()
    {
        var response = await _client.PostAsJsonAsync("/connector/company", _event);

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
        _monolith.GetCompanyAndAdminForCreatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((new CompanyCreateCompanyResult
            {
                Name = "Test", Email = "t@t.com", Website = "https://t.com", IndustryUId = Guid.NewGuid()
            }, new CompanyCreateUserResult
            {
                Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "j@t.com"
            }));

        _userApi.SendCompanyCreatedAsync(
                Arg.Any<EventDto<CompanyCreatedUserApiPayload>>(),
                Arg.Any<CancellationToken>())
            .Returns(new CompanyCreatedUserApiPayload
            {
                KeycloakUserId = "kc-user", KeycloakGroupId = "kc-group",
                CompanyName = "Test", FirstName = "John", LastName = "Doe",
                Email = "j@t.com", CompanyUId = _eventData.CompanyUId
            });

        var response = await _client.PostAsJsonAsync("/connector/company", _event);

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
        _monolith.GetCompanyAndAdminForCreatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Monolith unreachable"));

        var response = await _client.PostAsJsonAsync("/connector/company", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Should_NotSaveDoneState_WhenSagaThrows()
    {
        _monolith.GetCompanyAndAdminForCreatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Monolith unreachable"));

        await _client.PostAsJsonAsync("/connector/company", _event);

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
