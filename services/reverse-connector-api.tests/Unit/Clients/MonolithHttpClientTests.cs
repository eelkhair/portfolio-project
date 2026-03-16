using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Models;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Clients;

public class MonolithHttpClientTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test");
    private readonly ILogger<MonolithHttpClient> _logger = Substitute.For<ILogger<MonolithHttpClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithHttpClient _sut;

    public MonolithHttpClientTests()
    {
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://monolith.test/") };
        _sut = new MonolithHttpClient(httpClient, _activitySource, _logger);
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _handler.Dispose();
    }

    // ──── SyncDraftAsync ────

    [Fact]
    public async Task SyncDraftAsync_PostsToCorrectUrl()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncDraftPayload
        {
            DraftId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            ContentJson = "{\"title\":\"Test\"}"
        };

        await _sut.SyncDraftAsync(payload, "user-1", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/api/sync/drafts");
    }

    [Fact]
    public async Task SyncDraftAsync_IncludesUserIdInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncDraftPayload
        {
            DraftId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            ContentJson = "{}"
        };

        await _sut.SyncDraftAsync(payload, "sync-user", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("sync-user");
    }

    [Fact]
    public async Task SyncDraftAsync_ThrowsOnErrorResponse()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var payload = new SyncDraftPayload { DraftId = Guid.NewGuid(), CompanyId = Guid.NewGuid() };

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.SyncDraftAsync(payload, "user-1", CancellationToken.None));
    }

    // ──── DeleteDraftAsync ────

    [Fact]
    public async Task DeleteDraftAsync_SendsDeleteWithQueryParam()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var draftId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        await _sut.DeleteDraftAsync(draftId, companyId, "user-1", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Delete);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldContain($"/api/sync/drafts/{draftId}");
        _handler.LastRequest.RequestUri.Query.ShouldContain($"companyId={companyId}");
    }

    [Fact]
    public async Task DeleteDraftAsync_IncludesUserIdHeader()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        await _sut.DeleteDraftAsync(Guid.NewGuid(), Guid.NewGuid(), "delete-user", CancellationToken.None);

        _handler.LastRequest!.Headers.GetValues("x-user-id").ShouldContain("delete-user");
    }

    // ──── SyncCompanyCreateAsync ────

    [Fact]
    public async Task SyncCompanyCreateAsync_PostsToCorrectUrl()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncCompanyCreatePayload
        {
            CompanyId = Guid.NewGuid(),
            Name = "TestCorp",
            CompanyEmail = "test@corp.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@test.com"
        };

        await _sut.SyncCompanyCreateAsync(payload, "user-1", CancellationToken.None);

        _handler.LastRequest!.RequestUri!.PathAndQuery.ShouldBe("/api/sync/companies");
        _handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    // ──── SyncCompanyUpdateAsync ────

    [Fact]
    public async Task SyncCompanyUpdateAsync_PutsToCorrectUrl()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var companyId = Guid.NewGuid();
        var payload = new SyncCompanyUpdatePayload
        {
            CompanyId = companyId,
            Name = "Updated",
            CompanyEmail = "up@test.com",
            IndustryUId = Guid.NewGuid()
        };

        await _sut.SyncCompanyUpdateAsync(payload, "user-1", CancellationToken.None);

        _handler.LastRequest!.RequestUri!.PathAndQuery.ShouldBe($"/api/sync/companies/{companyId}");
        _handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
    }

    // ──── SyncJobCreateAsync ────

    [Fact]
    public async Task SyncJobCreateAsync_PostsToCorrectUrl()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncJobCreatePayload
        {
            JobId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Title = "Engineer",
            AboutRole = "Build",
            Location = "Remote",
            JobType = "FullTime"
        };

        await _sut.SyncJobCreateAsync(payload, "user-1", CancellationToken.None);

        _handler.LastRequest!.RequestUri!.PathAndQuery.ShouldBe("/api/sync/jobs");
        _handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
    }

    [Fact]
    public async Task SyncJobCreateAsync_IncludesPayloadFields()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncJobCreatePayload
        {
            JobId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Title = "Backend Dev",
            AboutRole = "APIs",
            Location = "NYC",
            JobType = "Contract",
            Responsibilities = ["Build APIs"],
            Qualifications = ["3+ years"]
        };

        await _sut.SyncJobCreateAsync(payload, "job-user", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Backend Dev");
        json.RootElement.GetProperty("userId").GetString().ShouldBe("job-user");
        json.RootElement.GetProperty("responsibilities").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task SyncJobCreateAsync_ThrowsOnErrorResponse()
    {
        _handler.SetResponse(HttpStatusCode.BadRequest);

        var payload = new SyncJobCreatePayload
        {
            JobId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Title = "Test",
            AboutRole = "Test",
            Location = "Test",
            JobType = "FullTime"
        };

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.SyncJobCreateAsync(payload, "user-1", CancellationToken.None));
    }
}

/// <summary>
/// Simple mock HTTP handler that captures the last request and returns a configurable response.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseContent = "{}";

    public HttpRequestMessage? LastRequest { get; private set; }

    public void SetResponse(HttpStatusCode statusCode, string content = "{}")
    {
        _statusCode = statusCode;
        _responseContent = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent)
        });
    }
}
