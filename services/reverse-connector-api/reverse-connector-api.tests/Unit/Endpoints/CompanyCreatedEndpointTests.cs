using System.Diagnostics;
using System.Net;
using System.Text.Json;
using JobBoard.IntegrationEvents.Company;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Mappers;
using ReverseConnectorAPI.Tests.Unit.Clients;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Endpoints;

public class CompanyCreatedEndpointTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test");
    private readonly ILogger<MonolithHttpClient> _logger = Substitute.For<ILogger<MonolithHttpClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithHttpClient _monolithClient;

    public CompanyCreatedEndpointTests()
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
    public void Mapper_MapsAllFieldsToCreatePayload()
    {
        var companyId = Guid.NewGuid();
        var industryId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userCompanyId = Guid.NewGuid();

        var evt = new MicroCompanyCreatedV1Event(
            CompanyUId: companyId,
            Name: "TestCorp",
            CompanyEmail: "hr@testcorp.com",
            CompanyWebsite: "https://testcorp.com",
            IndustryUId: industryId,
            AdminFirstName: "John",
            AdminLastName: "Doe",
            AdminEmail: "john@testcorp.com",
            AdminUId: adminId,
            UserCompanyUId: userCompanyId
        );

        var payload = CompanyMapper.ToCreatePayload(evt);

        payload.CompanyId.ShouldBe(companyId);
        payload.Name.ShouldBe("TestCorp");
        payload.CompanyEmail.ShouldBe("hr@testcorp.com");
        payload.CompanyWebsite.ShouldBe("https://testcorp.com");
        payload.IndustryUId.ShouldBe(industryId);
        payload.AdminFirstName.ShouldBe("John");
        payload.AdminLastName.ShouldBe("Doe");
        payload.AdminEmail.ShouldBe("john@testcorp.com");
        payload.AdminUId.ShouldBe(adminId);
        payload.UserCompanyUId.ShouldBe(userCompanyId);
    }

    [Fact]
    public async Task SyncCompanyCreate_PostsMappedPayloadToMonolith()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var evt = new MicroCompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "NewCorp",
            CompanyEmail: "info@newcorp.com",
            CompanyWebsite: "https://newcorp.com",
            IndustryUId: Guid.NewGuid(),
            AdminFirstName: "Alice",
            AdminLastName: "Smith",
            AdminEmail: "alice@newcorp.com",
            AdminUId: Guid.NewGuid(),
            UserCompanyUId: Guid.NewGuid()
        );

        var payload = CompanyMapper.ToCreatePayload(evt);
        await _monolithClient.SyncCompanyCreateAsync(payload, "user-123", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/api/sync/companies");
    }

    [Fact]
    public async Task SyncCompanyCreate_IncludesUserIdInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var evt = new MicroCompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "TestCorp",
            CompanyEmail: "test@corp.com",
            CompanyWebsite: null,
            IndustryUId: Guid.NewGuid(),
            AdminFirstName: "Bob",
            AdminLastName: "Jones",
            AdminEmail: "bob@corp.com",
            AdminUId: null,
            UserCompanyUId: null
        );

        var payload = CompanyMapper.ToCreatePayload(evt);
        await _monolithClient.SyncCompanyCreateAsync(payload, "sync-user-42", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("sync-user-42");
        json.RootElement.GetProperty("name").GetString().ShouldBe("TestCorp");
    }

    [Fact]
    public async Task SyncCompanyCreate_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var evt = new MicroCompanyCreatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "FailCorp",
            CompanyEmail: "fail@corp.com",
            CompanyWebsite: null,
            IndustryUId: Guid.NewGuid(),
            AdminFirstName: "Fail",
            AdminLastName: "User",
            AdminEmail: "fail@test.com",
            AdminUId: null,
            UserCompanyUId: null
        );

        var payload = CompanyMapper.ToCreatePayload(evt);

        await Should.ThrowAsync<HttpRequestException>(
            () => _monolithClient.SyncCompanyCreateAsync(payload, "user-1", CancellationToken.None));
    }
}
