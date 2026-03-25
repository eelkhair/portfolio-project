using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AH.Metadata.Domain.Constants;
using ConnectorAPI.Endpoints.Company;
using ConnectorAPI.Interfaces.Clients;
using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyUpdated;
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
public class CompanyUpdatedEndpointTests : IAsyncDisposable
{
    private const string IdempotencyPrefix = "Provisioned:";

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly IMonolithClient _monolith = Substitute.For<IMonolithClient>();
    private readonly ICompanyApiClient _companyApi = Substitute.For<ICompanyApiClient>();
    private readonly IJobApiClient _jobApi = Substitute.For<IJobApiClient>();
    private readonly ActivitySource _activitySource = new("test.company-updated");

    private readonly WebApplication _app;
    private readonly HttpClient _client;

    private readonly CompanyUpdatedV1Event _eventData;
    private readonly EventDto<CompanyUpdatedV1Event> _event;

    public CompanyUpdatedEndpointTests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(_daprClient);
        builder.Services.AddSingleton(_activitySource);
        builder.Services.AddSingleton(_monolith);
        builder.Services.AddSingleton(_companyApi);
        builder.Services.AddSingleton(_jobApi);
        builder.Services.AddSingleton(Substitute.For<ILogger<UpdateCompanySaga>>());
        builder.Services.AddScoped<UpdateCompanySaga>();

        _app = builder.Build();
        _app.MapCompanyUpdatedEndpoint();
        _app.StartAsync().GetAwaiter().GetResult();

        _client = _app.GetTestClient();

        _eventData = new CompanyUpdatedV1Event(
            CompanyUId: Guid.NewGuid(),
            IndustryUId: Guid.NewGuid())
        {
            UserId = "keycloak|user-456"
        };

        _event = new EventDto<CompanyUpdatedV1Event>(
            _eventData.UserId, Guid.NewGuid().ToString(), _eventData);

        // Default: state does not exist (not idempotent)
        _daprClient.GetStateAsync<string>(
                StateStores.Redis,
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((string?)null);

        // Default: monolith returns valid company data
        _monolith.GetCompanyForUpdatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyUpdateCompanyResult
            {
                Name = "TestCorp", Email = "t@t.com", Website = "https://t.com", IndustryUId = Guid.NewGuid()
            });
    }

    [Fact]
    public async Task Should_CallSaga_WhenNotIdempotent()
    {
        var response = await _client.PostAsJsonAsync("/connector/company-updated", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _monolith.Received(1).GetCompanyForUpdatedEventAsync(
            _eventData.CompanyUId,
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

        var response = await _client.PostAsJsonAsync("/connector/company-updated", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _monolith.DidNotReceive().GetCompanyForUpdatedEventAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SavePendingState_BeforeCallingSaga()
    {
        var response = await _client.PostAsJsonAsync("/connector/company-updated", _event);

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
        var response = await _client.PostAsJsonAsync("/connector/company-updated", _event);

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
        _monolith.GetCompanyForUpdatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Monolith unreachable"));

        var response = await _client.PostAsJsonAsync("/connector/company-updated", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Should_NotSaveDoneState_WhenSagaThrows()
    {
        _monolith.GetCompanyForUpdatedEventAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Monolith unreachable"));

        await _client.PostAsJsonAsync("/connector/company-updated", _event);

        await _daprClient.DidNotReceive().SaveStateAsync(
            StateStores.Redis,
            Arg.Any<string>(),
            "done",
            metadata: Arg.Any<Dictionary<string, string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_FanOutToCompanyApiAndJobApi()
    {
        var response = await _client.PostAsJsonAsync("/connector/company-updated", _event);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await _companyApi.Received(1).SendCompanyUpdatedAsync(
            _eventData.CompanyUId,
            Arg.Any<CompanyUpdatedCompanyApiPayload>(),
            Arg.Any<CancellationToken>());
        await _jobApi.Received(1).SendCompanyUpdatedAsync(
            _eventData.CompanyUId,
            Arg.Any<EventDto<CompanyUpdatedJobApiPayload>>(),
            Arg.Any<CancellationToken>());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _activitySource.Dispose();
    }
}
