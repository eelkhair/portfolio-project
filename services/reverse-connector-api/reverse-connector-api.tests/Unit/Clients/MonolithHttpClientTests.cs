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

    // ──── SyncDraftAsync body content ────

    [Fact]
    public async Task SyncDraftAsync_IncludesAllPayloadFieldsInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var draftId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var payload = new SyncDraftPayload
        {
            DraftId = draftId,
            CompanyId = companyId,
            ContentJson = "{\"title\":\"Engineer\",\"location\":\"Remote\"}"
        };

        await _sut.SyncDraftAsync(payload, "body-user", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("draftId").GetString().ShouldBe(draftId.ToString());
        json.RootElement.GetProperty("companyId").GetString().ShouldBe(companyId.ToString());
        json.RootElement.GetProperty("contentJson").GetString().ShouldContain("Engineer");
    }

    // ──── DeleteDraftAsync error ────

    [Fact]
    public async Task DeleteDraftAsync_ThrowsOnNotFound()
    {
        _handler.SetResponse(HttpStatusCode.NotFound);

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.DeleteDraftAsync(Guid.NewGuid(), Guid.NewGuid(), "user-1", CancellationToken.None));
    }

    // ──── SyncCompanyCreateAsync body content ────

    [Fact]
    public async Task SyncCompanyCreateAsync_IncludesAdminFieldsInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var adminId = Guid.NewGuid();
        var userCompanyId = Guid.NewGuid();
        var payload = new SyncCompanyCreatePayload
        {
            CompanyId = Guid.NewGuid(),
            Name = "BodyCorp",
            CompanyEmail = "body@corp.com",
            CompanyWebsite = "https://body.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "Alice",
            AdminLastName = "Wonder",
            AdminEmail = "alice@body.com",
            AdminUId = adminId,
            UserCompanyUId = userCompanyId
        };

        await _sut.SyncCompanyCreateAsync(payload, "admin-user", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("admin-user");
        json.RootElement.GetProperty("adminFirstName").GetString().ShouldBe("Alice");
        json.RootElement.GetProperty("adminLastName").GetString().ShouldBe("Wonder");
        json.RootElement.GetProperty("adminEmail").GetString().ShouldBe("alice@body.com");
        json.RootElement.GetProperty("adminUId").GetString().ShouldBe(adminId.ToString());
        json.RootElement.GetProperty("userCompanyUId").GetString().ShouldBe(userCompanyId.ToString());
    }

    [Fact]
    public async Task SyncCompanyCreateAsync_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var payload = new SyncCompanyCreatePayload
        {
            CompanyId = Guid.NewGuid(),
            Name = "FailCorp",
            CompanyEmail = "fail@corp.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "Fail",
            AdminLastName = "User",
            AdminEmail = "fail@test.com"
        };

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.SyncCompanyCreateAsync(payload, "user-1", CancellationToken.None));
    }

    [Fact]
    public async Task SyncCompanyCreateAsync_WithNullOptionalFields_SerializesNulls()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncCompanyCreatePayload
        {
            CompanyId = Guid.NewGuid(),
            Name = "MinCorp",
            CompanyEmail = "min@corp.com",
            CompanyWebsite = null,
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "Min",
            AdminLastName = "User",
            AdminEmail = "min@test.com",
            AdminUId = null,
            UserCompanyUId = null
        };

        await _sut.SyncCompanyCreateAsync(payload, "user-1", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("companyWebsite").ValueKind.ShouldBe(JsonValueKind.Null);
        json.RootElement.GetProperty("adminUId").ValueKind.ShouldBe(JsonValueKind.Null);
        json.RootElement.GetProperty("userCompanyUId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    // ──── SyncCompanyUpdateAsync body content ────

    [Fact]
    public async Task SyncCompanyUpdateAsync_IncludesAllFieldsInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var companyId = Guid.NewGuid();
        var industryId = Guid.NewGuid();
        var founded = new DateTime(2019, 3, 15);
        var payload = new SyncCompanyUpdatePayload
        {
            CompanyId = companyId,
            Name = "FullCorp",
            CompanyEmail = "full@corp.com",
            CompanyWebsite = "https://full.com",
            Phone = "+1112223333",
            Description = "Full desc",
            About = "Full about",
            EEO = "Full EEO",
            Founded = founded,
            Size = "100-500",
            Logo = "https://cdn.test/full.png",
            IndustryUId = industryId
        };

        await _sut.SyncCompanyUpdateAsync(payload, "full-user", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("full-user");
        json.RootElement.GetProperty("name").GetString().ShouldBe("FullCorp");
        json.RootElement.GetProperty("companyEmail").GetString().ShouldBe("full@corp.com");
        json.RootElement.GetProperty("companyWebsite").GetString().ShouldBe("https://full.com");
        json.RootElement.GetProperty("phone").GetString().ShouldBe("+1112223333");
        json.RootElement.GetProperty("description").GetString().ShouldBe("Full desc");
        json.RootElement.GetProperty("about").GetString().ShouldBe("Full about");
        json.RootElement.GetProperty("eeo").GetString().ShouldBe("Full EEO");
        json.RootElement.GetProperty("size").GetString().ShouldBe("100-500");
        json.RootElement.GetProperty("logo").GetString().ShouldBe("https://cdn.test/full.png");
    }

    [Fact]
    public async Task SyncCompanyUpdateAsync_ThrowsOnBadRequest()
    {
        _handler.SetResponse(HttpStatusCode.BadRequest);

        var payload = new SyncCompanyUpdatePayload
        {
            CompanyId = Guid.NewGuid(),
            Name = "BadCorp",
            CompanyEmail = "bad@corp.com",
            IndustryUId = Guid.NewGuid()
        };

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.SyncCompanyUpdateAsync(payload, "user-1", CancellationToken.None));
    }

    // ──── SyncJobCreateAsync body verification with empty collections ────

    [Fact]
    public async Task SyncJobCreateAsync_WithEmptyCollections_SerializesEmptyArrays()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var payload = new SyncJobCreatePayload
        {
            JobId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Title = "Minimal Job",
            AboutRole = "Minimal role",
            Location = "Anywhere",
            SalaryRange = null,
            JobType = "FullTime",
            Responsibilities = [],
            Qualifications = []
        };

        await _sut.SyncJobCreateAsync(payload, "user-1", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("responsibilities").GetArrayLength().ShouldBe(0);
        json.RootElement.GetProperty("qualifications").GetArrayLength().ShouldBe(0);
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
