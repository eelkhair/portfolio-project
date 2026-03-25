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

public class CompanyUpdatedEndpointTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test");
    private readonly ILogger<MonolithHttpClient> _logger = Substitute.For<ILogger<MonolithHttpClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithHttpClient _monolithClient;

    public CompanyUpdatedEndpointTests()
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
    public void Mapper_MapsAllFieldsToUpdatePayload()
    {
        var companyId = Guid.NewGuid();
        var industryId = Guid.NewGuid();
        var founded = new DateTime(2018, 6, 15);

        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: companyId,
            Name: "UpdatedCorp",
            CompanyEmail: "info@updated.com",
            CompanyWebsite: "https://updated.com",
            Phone: "+1234567890",
            Description: "A great company",
            About: "We build things",
            EEO: "Equal opportunity employer",
            Founded: founded,
            Size: "50-200",
            Logo: "https://cdn.test/logo.png",
            IndustryUId: industryId
        );

        var payload = CompanyMapper.ToUpdatePayload(evt);

        payload.CompanyId.ShouldBe(companyId);
        payload.Name.ShouldBe("UpdatedCorp");
        payload.CompanyEmail.ShouldBe("info@updated.com");
        payload.CompanyWebsite.ShouldBe("https://updated.com");
        payload.Phone.ShouldBe("+1234567890");
        payload.Description.ShouldBe("A great company");
        payload.About.ShouldBe("We build things");
        payload.EEO.ShouldBe("Equal opportunity employer");
        payload.Founded.ShouldBe(founded);
        payload.Size.ShouldBe("50-200");
        payload.Logo.ShouldBe("https://cdn.test/logo.png");
        payload.IndustryUId.ShouldBe(industryId);
    }

    [Fact]
    public async Task SyncCompanyUpdate_PutsMappedPayloadToMonolith()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var companyId = Guid.NewGuid();
        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: companyId,
            Name: "PutCorp",
            CompanyEmail: "put@corp.com",
            CompanyWebsite: null,
            Phone: null,
            Description: null,
            About: null,
            EEO: null,
            Founded: null,
            Size: null,
            Logo: null,
            IndustryUId: Guid.NewGuid()
        );

        var payload = CompanyMapper.ToUpdatePayload(evt);
        await _monolithClient.SyncCompanyUpdateAsync(payload, "user-88", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe($"/api/sync/companies/{companyId}");
    }

    [Fact]
    public async Task SyncCompanyUpdate_IncludesUserIdAndFieldsInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var companyId = Guid.NewGuid();
        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: companyId,
            Name: "BodyTestCorp",
            CompanyEmail: "body@test.com",
            CompanyWebsite: "https://body.test",
            Phone: "+9876543210",
            Description: "Test description",
            About: "Test about",
            EEO: "Test EEO",
            Founded: new DateTime(2020, 1, 1),
            Size: "10-50",
            Logo: "https://cdn.test/logo2.png",
            IndustryUId: Guid.NewGuid()
        );

        var payload = CompanyMapper.ToUpdatePayload(evt);
        await _monolithClient.SyncCompanyUpdateAsync(payload, "update-user-11", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("update-user-11");
        json.RootElement.GetProperty("name").GetString().ShouldBe("BodyTestCorp");
        json.RootElement.GetProperty("companyEmail").GetString().ShouldBe("body@test.com");
        json.RootElement.GetProperty("phone").GetString().ShouldBe("+9876543210");
    }

    [Fact]
    public async Task SyncCompanyUpdate_WithNullOptionalFields_SerializesNulls()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "MinimalCorp",
            CompanyEmail: "min@corp.com",
            CompanyWebsite: null,
            Phone: null,
            Description: null,
            About: null,
            EEO: null,
            Founded: null,
            Size: null,
            Logo: null,
            IndustryUId: Guid.NewGuid()
        );

        var payload = CompanyMapper.ToUpdatePayload(evt);
        await _monolithClient.SyncCompanyUpdateAsync(payload, "user-1", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("companyWebsite").ValueKind.ShouldBe(JsonValueKind.Null);
        json.RootElement.GetProperty("phone").ValueKind.ShouldBe(JsonValueKind.Null);
        json.RootElement.GetProperty("description").ValueKind.ShouldBe(JsonValueKind.Null);
        json.RootElement.GetProperty("logo").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task SyncCompanyUpdate_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var evt = new MicroCompanyUpdatedV1Event(
            CompanyUId: Guid.NewGuid(),
            Name: "FailCorp",
            CompanyEmail: "fail@corp.com",
            CompanyWebsite: null,
            Phone: null,
            Description: null,
            About: null,
            EEO: null,
            Founded: null,
            Size: null,
            Logo: null,
            IndustryUId: Guid.NewGuid()
        );

        var payload = CompanyMapper.ToUpdatePayload(evt);

        await Should.ThrowAsync<HttpRequestException>(
            () => _monolithClient.SyncCompanyUpdateAsync(payload, "user-1", CancellationToken.None));
    }
}
