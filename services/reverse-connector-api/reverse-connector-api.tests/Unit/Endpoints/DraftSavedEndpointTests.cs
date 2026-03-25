using System.Diagnostics;
using System.Net;
using System.Text.Json;
using JobBoard.IntegrationEvents.Draft;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Mappers;
using ReverseConnectorAPI.Tests.Unit.Clients;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Endpoints;

public class DraftSavedEndpointTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test");
    private readonly ILogger<MonolithHttpClient> _logger = Substitute.For<ILogger<MonolithHttpClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithHttpClient _monolithClient;

    public DraftSavedEndpointTests()
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
    public void Mapper_ProducesValidContentJson()
    {
        var draftId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var evt = new DraftSavedV1Event(
            UId: draftId,
            CompanyUId: companyId,
            Title: "Senior Engineer",
            AboutRole: "Build systems",
            Location: "Remote",
            JobType: "FullTime",
            SalaryRange: "$100k-$150k",
            Notes: "Priority hire",
            Responsibilities: ["Design APIs", "Code reviews"],
            Qualifications: ["5+ years C#"]
        );

        var payload = DraftMapper.ToPayload(evt);

        payload.DraftId.ShouldBe(draftId);
        payload.CompanyId.ShouldBe(companyId);
        payload.ContentJson.ShouldNotBeNullOrEmpty();

        var json = JsonDocument.Parse(payload.ContentJson);
        json.RootElement.GetProperty("title").GetString().ShouldBe("Senior Engineer");
        json.RootElement.GetProperty("aboutRole").GetString().ShouldBe("Build systems");
        json.RootElement.GetProperty("location").GetString().ShouldBe("Remote");
        json.RootElement.GetProperty("jobType").GetString().ShouldBe("FullTime");
        json.RootElement.GetProperty("salaryRange").GetString().ShouldBe("$100k-$150k");
        json.RootElement.GetProperty("notes").GetString().ShouldBe("Priority hire");
        json.RootElement.GetProperty("responsibilities").GetArrayLength().ShouldBe(2);
        json.RootElement.GetProperty("qualifications").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task SyncDraft_PostsMappedPayloadToMonolith()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var evt = new DraftSavedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Backend Dev",
            AboutRole: "APIs",
            Location: "NYC",
            JobType: "Contract",
            SalaryRange: null,
            Notes: "",
            Responsibilities: ["Build APIs"],
            Qualifications: ["3+ years"]
        );

        var payload = DraftMapper.ToPayload(evt);
        await _monolithClient.SyncDraftAsync(payload, "user-99", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/api/sync/drafts");
    }

    [Fact]
    public async Task SyncDraft_IncludesUserIdAndContentJsonInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var draftId = Guid.NewGuid();
        var evt = new DraftSavedV1Event(
            UId: draftId,
            CompanyUId: Guid.NewGuid(),
            Title: "Test Draft",
            AboutRole: "Test Role",
            Location: "Anywhere",
            JobType: "FullTime",
            SalaryRange: null,
            Notes: "",
            Responsibilities: [],
            Qualifications: []
        );

        var payload = DraftMapper.ToPayload(evt);
        await _monolithClient.SyncDraftAsync(payload, "draft-user", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("draft-user");
        json.RootElement.GetProperty("contentJson").GetString().ShouldNotBeNullOrEmpty();
        json.RootElement.GetProperty("draftId").GetString().ShouldBe(draftId.ToString());
    }

    [Fact]
    public async Task SyncDraft_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.InternalServerError);

        var evt = new DraftSavedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Fail",
            AboutRole: "Fail",
            Location: "Fail",
            JobType: "FullTime",
            SalaryRange: null,
            Notes: "",
            Responsibilities: [],
            Qualifications: []
        );

        var payload = DraftMapper.ToPayload(evt);

        await Should.ThrowAsync<HttpRequestException>(
            () => _monolithClient.SyncDraftAsync(payload, "user-1", CancellationToken.None));
    }
}
