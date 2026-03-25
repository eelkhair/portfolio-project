using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Tests.Unit.Clients;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Endpoints;

public class DraftDeletedEndpointTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test");
    private readonly ILogger<MonolithHttpClient> _logger = Substitute.For<ILogger<MonolithHttpClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithHttpClient _monolithClient;

    public DraftDeletedEndpointTests()
    {
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://monolith.test/") };
        _monolithClient = new MonolithHttpClient(httpClient, _activitySource, _logger);
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task DeleteDraft_SendsDeleteToCorrectUrl()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var draftId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        await _monolithClient.DeleteDraftAsync(draftId, companyId, "user-1", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Delete);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldContain($"/api/sync/drafts/{draftId}");
        _handler.LastRequest.RequestUri.Query.ShouldContain($"companyId={companyId}");
    }

    [Fact]
    public async Task DeleteDraft_IncludesUserIdHeader()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        await _monolithClient.DeleteDraftAsync(
            Guid.NewGuid(), Guid.NewGuid(), "delete-user-33", CancellationToken.None);

        _handler.LastRequest!.Headers.GetValues("x-user-id").ShouldContain("delete-user-33");
    }

    [Fact]
    public async Task DeleteDraft_WithDifferentCompanyIds_RoutesCorrectly()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var draftId = Guid.NewGuid();
        var companyIdA = Guid.NewGuid();
        var companyIdB = Guid.NewGuid();

        await _monolithClient.DeleteDraftAsync(draftId, companyIdA, "user-1", CancellationToken.None);
        _handler.LastRequest!.RequestUri!.Query.ShouldContain($"companyId={companyIdA}");

        await _monolithClient.DeleteDraftAsync(draftId, companyIdB, "user-1", CancellationToken.None);
        _handler.LastRequest!.RequestUri!.Query.ShouldContain($"companyId={companyIdB}");
    }

    [Fact]
    public async Task DeleteDraft_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        await Should.ThrowAsync<HttpRequestException>(
            () => _monolithClient.DeleteDraftAsync(
                Guid.NewGuid(), Guid.NewGuid(), "user-1", CancellationToken.None));
    }
}
