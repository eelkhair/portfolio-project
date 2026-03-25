using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Services;
using JobBoard.IntegrationEvents.Company;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace connector_api.tests.Unit.Services;

[Trait("Category", "Unit")]
public class MonolithClientTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test.monolith-client");
    private readonly ILogger<MonolithOClient> _logger = Substitute.For<ILogger<MonolithOClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithOClient _sut;

    public MonolithClientTests()
    {
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://monolith.test/") };
        _sut = new MonolithOClient(httpClient, _activitySource, _logger);
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        _handler.Dispose();
    }

    // ──── GetCompanyAndAdminForCreatedEventAsync ────

    [Fact]
    public async Task GetCompanyAndAdminForCreatedEventAsync_FetchesBothCompanyAndAdmin()
    {
        var companyId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var company = new CompanyCreateCompanyResult
        {
            Name = "TestCorp", Email = "t@t.com", Website = "https://t.com", IndustryUId = Guid.NewGuid()
        };
        var admin = new CompanyCreateUserResult
        {
            Id = adminId, FirstName = "John", LastName = "Doe", Email = "j@t.com"
        };

        _handler.SetSequentialResponses(
            (HttpStatusCode.OK, JsonSerializer.Serialize(company, JsonOpts)),
            (HttpStatusCode.OK, JsonSerializer.Serialize(admin, JsonOpts)));

        var (resultCompany, resultAdmin) = await _sut.GetCompanyAndAdminForCreatedEventAsync(
            companyId, adminId, "user-1", CancellationToken.None);

        resultCompany.Name.ShouldBe("TestCorp");
        resultCompany.Email.ShouldBe("t@t.com");
        resultAdmin.FirstName.ShouldBe("John");
        resultAdmin.LastName.ShouldBe("Doe");
    }

    [Fact]
    public async Task GetCompanyAndAdminForCreatedEventAsync_SetsUserIdHeader()
    {
        var company = new CompanyCreateCompanyResult { Name = "C", Email = "e@e.com" };
        var admin = new CompanyCreateUserResult { FirstName = "A", LastName = "B", Email = "a@b.com" };

        _handler.SetSequentialResponses(
            (HttpStatusCode.OK, JsonSerializer.Serialize(company, JsonOpts)),
            (HttpStatusCode.OK, JsonSerializer.Serialize(admin, JsonOpts)));

        await _sut.GetCompanyAndAdminForCreatedEventAsync(
            Guid.NewGuid(), Guid.NewGuid(), "custom-user-id", CancellationToken.None);

        _handler.AllRequests.ShouldAllBe(r =>
            r.Headers.Contains("x-user-id") &&
            r.Headers.GetValues("x-user-id").Contains("custom-user-id"));
    }

    [Fact]
    public async Task GetCompanyAndAdminForCreatedEventAsync_DoesNotSetUserIdHeader_WhenEmpty()
    {
        var company = new CompanyCreateCompanyResult { Name = "C", Email = "e@e.com" };
        var admin = new CompanyCreateUserResult { FirstName = "A", LastName = "B", Email = "a@b.com" };

        _handler.SetSequentialResponses(
            (HttpStatusCode.OK, JsonSerializer.Serialize(company, JsonOpts)),
            (HttpStatusCode.OK, JsonSerializer.Serialize(admin, JsonOpts)));

        await _sut.GetCompanyAndAdminForCreatedEventAsync(
            Guid.NewGuid(), Guid.NewGuid(), "", CancellationToken.None);

        _handler.AllRequests.ShouldAllBe(r => !r.Headers.Contains("x-user-id"));
    }

    [Fact]
    public async Task GetCompanyAndAdminForCreatedEventAsync_SendsODataRouteWithSelect()
    {
        var companyId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var company = new CompanyCreateCompanyResult { Name = "C", Email = "e@e.com" };
        var admin = new CompanyCreateUserResult { FirstName = "A", LastName = "B", Email = "a@b.com" };

        _handler.SetSequentialResponses(
            (HttpStatusCode.OK, JsonSerializer.Serialize(company, JsonOpts)),
            (HttpStatusCode.OK, JsonSerializer.Serialize(admin, JsonOpts)));

        await _sut.GetCompanyAndAdminForCreatedEventAsync(
            companyId, adminId, "user-1", CancellationToken.None);

        _handler.AllRequests.Count.ShouldBe(2);
        var companyRequest = _handler.AllRequests.First(r =>
            r.RequestUri!.PathAndQuery.Contains("companies"));
        companyRequest.RequestUri!.PathAndQuery.ShouldContain($"odata/companies({companyId})");
        companyRequest.RequestUri.Query.ShouldContain("select");

        var adminRequest = _handler.AllRequests.First(r =>
            r.RequestUri!.PathAndQuery.Contains("users"));
        adminRequest.RequestUri!.PathAndQuery.ShouldContain($"odata/users({adminId})");
        adminRequest.RequestUri.Query.ShouldContain("select");
    }

    [Fact]
    public async Task GetCompanyAndAdminForCreatedEventAsync_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.GetCompanyAndAdminForCreatedEventAsync(
                Guid.NewGuid(), Guid.NewGuid(), "user-1", CancellationToken.None));
    }

    // ──── ActivateCompanyAsync ────

    [Fact]
    public async Task ActivateCompanyAsync_PostsToCorrectUrl()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var eventData = new CompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            IndustryUId: Guid.NewGuid(),
            AdminUId: Guid.NewGuid(),
            UserCompanyUId: Guid.NewGuid())
        { UserId = "user-1" };

        var company = new CompanyCreateCompanyResult
        {
            Name = "TestCorp", Email = "test@corp.com"
        };

        var userApiResponse = new CompanyCreatedUserApiPayload
        {
            KeycloakGroupId = "kc-group", KeycloakUserId = "kc-user",
            CompanyName = "TestCorp", FirstName = "John", LastName = "Doe",
            Email = "john@test.com", CompanyUId = eventData.CompanyUId
        };

        await _sut.ActivateCompanyAsync(eventData, company, userApiResponse, CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/api/companies/company-created-success");
    }

    [Fact]
    public async Task ActivateCompanyAsync_IncludesCorrectPayloadInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var companyUId = Guid.NewGuid();
        var adminUId = Guid.NewGuid();

        var eventData = new CompanyCreatedV1Event(
            CompanyUId: companyUId,
            IndustryUId: Guid.NewGuid(),
            AdminUId: adminUId,
            UserCompanyUId: Guid.NewGuid())
        { UserId = "creator-user" };

        var company = new CompanyCreateCompanyResult
        {
            Name = "ActivateCorp", Email = "activate@corp.com"
        };

        var userApiResponse = new CompanyCreatedUserApiPayload
        {
            KeycloakGroupId = "group-123", KeycloakUserId = "user-456",
            CompanyName = "ActivateCorp", FirstName = "Jane", LastName = "Smith",
            Email = "jane@test.com", CompanyUId = companyUId
        };

        await _sut.ActivateCompanyAsync(eventData, company, userApiResponse, CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("keycloakGroupId").GetString().ShouldBe("group-123");
        json.RootElement.GetProperty("keycloakUserId").GetString().ShouldBe("user-456");
        json.RootElement.GetProperty("companyName").GetString().ShouldBe("ActivateCorp");
        json.RootElement.GetProperty("companyEmail").GetString().ShouldBe("activate@corp.com");
        json.RootElement.GetProperty("companyUId").GetString().ShouldBe(companyUId.ToString());
        json.RootElement.GetProperty("userUId").GetString().ShouldBe(adminUId.ToString());
        json.RootElement.GetProperty("createdBy").GetString().ShouldBe("creator-user");
    }

    [Fact]
    public async Task ActivateCompanyAsync_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var eventData = new CompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            IndustryUId: Guid.NewGuid(),
            AdminUId: Guid.NewGuid(),
            UserCompanyUId: Guid.NewGuid())
        { UserId = "user-1" };

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.ActivateCompanyAsync(
                eventData,
                new CompanyCreateCompanyResult { Name = "C", Email = "c@c.com" },
                new CompanyCreatedUserApiPayload(),
                CancellationToken.None));
    }

    // ──── GetCompanyForUpdatedEventAsync ────

    [Fact]
    public async Task GetCompanyForUpdatedEventAsync_FetchesCompanyFromOData()
    {
        var companyId = Guid.NewGuid();
        var result = new CompanyUpdateCompanyResult
        {
            Name = "UpdatedCorp",
            Email = "updated@corp.com",
            Website = "https://updated.com",
            Phone = "+1234567890",
            Description = "A company",
            About = "About us",
            EEO = "Equal opportunity",
            Founded = new DateTime(2020, 1, 1),
            Size = "50-200",
            Logo = "https://cdn.test/logo.png",
            IndustryUId = Guid.NewGuid()
        };

        _handler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(result, JsonOpts));

        var company = await _sut.GetCompanyForUpdatedEventAsync(companyId, "user-1", CancellationToken.None);

        company.Name.ShouldBe("UpdatedCorp");
        company.Email.ShouldBe("updated@corp.com");
        company.Website.ShouldBe("https://updated.com");
        company.Phone.ShouldBe("+1234567890");
        _handler.LastRequest!.RequestUri!.PathAndQuery.ShouldContain($"odata/companies({companyId})");
        _handler.LastRequest.RequestUri.Query.ShouldContain("select");
    }

    [Fact]
    public async Task GetCompanyForUpdatedEventAsync_SetsUserIdHeader()
    {
        var result = new CompanyUpdateCompanyResult { Name = "C", Email = "c@c.com" };
        _handler.SetResponse(HttpStatusCode.OK, JsonSerializer.Serialize(result, JsonOpts));

        await _sut.GetCompanyForUpdatedEventAsync(Guid.NewGuid(), "update-user", CancellationToken.None);

        _handler.LastRequest!.Headers.GetValues("x-user-id").ShouldContain("update-user");
    }

    [Fact]
    public async Task GetCompanyForUpdatedEventAsync_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.GetCompanyForUpdatedEventAsync(
                Guid.NewGuid(), "user-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetCompanyForUpdatedEventAsync_ThrowsOnNotFound()
    {
        _handler.SetResponse(HttpStatusCode.NotFound);

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.GetCompanyForUpdatedEventAsync(
                Guid.NewGuid(), "user-1", CancellationToken.None));
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Mock HTTP handler that captures all requests and returns configurable responses.
/// Supports sequential responses for parallel request scenarios.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _responseContent = "{}";
    private readonly List<(HttpStatusCode StatusCode, string Content)> _sequentialResponses = [];
    private int _responseIndex;

    public HttpRequestMessage? LastRequest { get; private set; }
    public List<HttpRequestMessage> AllRequests { get; } = [];

    public void SetResponse(HttpStatusCode statusCode, string content = "{}")
    {
        _statusCode = statusCode;
        _responseContent = content;
        _sequentialResponses.Clear();
    }

    public void SetSequentialResponses(params (HttpStatusCode StatusCode, string Content)[] responses)
    {
        _sequentialResponses.Clear();
        _sequentialResponses.AddRange(responses);
        _responseIndex = 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        AllRequests.Add(request);

        if (_sequentialResponses.Count > 0)
        {
            var idx = Interlocked.Increment(ref _responseIndex) - 1;
            if (idx < _sequentialResponses.Count)
            {
                var (code, content) = _sequentialResponses[idx];
                return Task.FromResult(new HttpResponseMessage(code)
                {
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
