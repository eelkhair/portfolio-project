using System.Diagnostics;
using System.Net;
using System.Text.Json;
using JobBoard.IntegrationEvents.Job;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ReverseConnectorAPI.Clients;
using ReverseConnectorAPI.Mappers;
using ReverseConnectorAPI.Tests.Unit.Clients;
using Shouldly;

namespace ReverseConnectorAPI.Tests.Unit.Endpoints;

public class JobCreatedEndpointTests : IDisposable
{
    private readonly ActivitySource _activitySource = new("test");
    private readonly ILogger<MonolithHttpClient> _logger = Substitute.For<ILogger<MonolithHttpClient>>();
    private readonly MockHttpMessageHandler _handler = new();
    private readonly MonolithHttpClient _monolithClient;

    public JobCreatedEndpointTests()
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
    public void Mapper_MapsAllFieldsToPayload()
    {
        var jobId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var evt = new MicroJobCreatedV1Event(
            UId: jobId,
            CompanyUId: companyId,
            Title: "Staff Engineer",
            AboutRole: "Lead architecture decisions",
            Location: "Remote",
            SalaryRange: "$200k-$250k",
            JobType: "FullTime",
            Responsibilities: ["System design", "Mentoring", "Code reviews"],
            Qualifications: ["10+ years", "Distributed systems"]
        );

        var payload = JobMapper.ToPayload(evt);

        payload.JobId.ShouldBe(jobId);
        payload.CompanyId.ShouldBe(companyId);
        payload.Title.ShouldBe("Staff Engineer");
        payload.AboutRole.ShouldBe("Lead architecture decisions");
        payload.Location.ShouldBe("Remote");
        payload.SalaryRange.ShouldBe("$200k-$250k");
        payload.JobType.ShouldBe("FullTime");
        payload.Responsibilities.ShouldBe(["System design", "Mentoring", "Code reviews"]);
        payload.Qualifications.ShouldBe(["10+ years", "Distributed systems"]);
    }

    [Fact]
    public async Task SyncJobCreate_PostsMappedPayloadToMonolith()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var evt = new MicroJobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Backend Dev",
            AboutRole: "Build APIs",
            Location: "NYC",
            SalaryRange: null,
            JobType: "Contract",
            Responsibilities: ["Build APIs"],
            Qualifications: ["3+ years"]
        );

        var payload = JobMapper.ToPayload(evt);
        await _monolithClient.SyncJobCreateAsync(payload, "user-77", CancellationToken.None);

        _handler.LastRequest.ShouldNotBeNull();
        _handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        _handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/api/sync/jobs");
    }

    [Fact]
    public async Task SyncJobCreate_IncludesAllFieldsInBody()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var jobId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        var evt = new MicroJobCreatedV1Event(
            UId: jobId,
            CompanyUId: companyId,
            Title: "DevOps Engineer",
            AboutRole: "Infrastructure",
            Location: "Berlin",
            SalaryRange: "$120k",
            JobType: "FullTime",
            Responsibilities: ["CI/CD", "Monitoring"],
            Qualifications: ["Kubernetes", "Terraform"]
        );

        var payload = JobMapper.ToPayload(evt);
        await _monolithClient.SyncJobCreateAsync(payload, "job-user-55", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("userId").GetString().ShouldBe("job-user-55");
        json.RootElement.GetProperty("title").GetString().ShouldBe("DevOps Engineer");
        json.RootElement.GetProperty("jobId").GetString().ShouldBe(jobId.ToString());
        json.RootElement.GetProperty("companyId").GetString().ShouldBe(companyId.ToString());
        json.RootElement.GetProperty("responsibilities").GetArrayLength().ShouldBe(2);
        json.RootElement.GetProperty("qualifications").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task SyncJobCreate_WithNullSalaryRange_SerializesNull()
    {
        _handler.SetResponse(HttpStatusCode.OK);

        var evt = new MicroJobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Intern",
            AboutRole: "Learn",
            Location: "On-site",
            SalaryRange: null,
            JobType: "Internship",
            Responsibilities: [],
            Qualifications: []
        );

        var payload = JobMapper.ToPayload(evt);
        await _monolithClient.SyncJobCreateAsync(payload, "user-1", CancellationToken.None);

        var body = await _handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("salaryRange").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task SyncJobCreate_ThrowsOnServerError()
    {
        _handler.SetResponse(HttpStatusCode.BadRequest);

        var evt = new MicroJobCreatedV1Event(
            UId: Guid.NewGuid(),
            CompanyUId: Guid.NewGuid(),
            Title: "Fail",
            AboutRole: "Fail",
            Location: "Fail",
            SalaryRange: null,
            JobType: "FullTime",
            Responsibilities: [],
            Qualifications: []
        );

        var payload = JobMapper.ToPayload(evt);

        await Should.ThrowAsync<HttpRequestException>(
            () => _monolithClient.SyncJobCreateAsync(payload, "user-1", CancellationToken.None));
    }
}
