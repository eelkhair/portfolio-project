using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Queries;
using AdminApi.Tests.Helpers;
using AdminAPI.Contracts.Models.Jobs.Responses;
using AdminAPI.Contracts.Services;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using JobAPI.Contracts.Models.Jobs.Responses;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace AdminApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class JobQueryServiceTests
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly ILogger<JobQueryService> _logger = Substitute.For<ILogger<JobQueryService>>();
    private readonly JobQueryService _sut;

    public JobQueryServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new JobQueryService(_daprClient, _accessor, _logger);
    }

    #region ListAsync

    [Fact]
    public async Task ListAsync_Success_ReturnsJobList()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        var jobs = new List<JobResponse>
        {
            new()
            {
                UId = Guid.NewGuid(), Title = "Engineer", CompanyUId = companyUId,
                CompanyName = "Corp", Location = "NYC",
                JobType = JobType.FullTime, AboutRole = "Build things",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                UId = Guid.NewGuid(), Title = "Designer", CompanyUId = companyUId,
                CompanyName = "Corp", Location = "Remote",
                JobType = JobType.Contract, AboutRole = "Design things",
                CreatedAt = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(jobs, _jsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.ListAsync(companyUId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        result.Data[0].Title.ShouldBe("Engineer");
        result.Data[1].Title.ShouldBe("Designer");
    }

    [Fact]
    public async Task ListAsync_WhenJobApiReturnsError_ReturnsFailure()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        var errorJson = JsonSerializer.Serialize(new ApiError { Message = "Not found" }, _jsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
        };

        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.ListAsync(companyUId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListAsync_WhenExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        _daprClient.SetupInvokeMethodWithException(new HttpRequestException("Connection failed"));

        // Act
        var result = await _sut.ListAsync(companyUId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region ListCompanyJobSummariesAsync

    [Fact]
    public async Task ListCompanyJobSummariesAsync_Success_ReturnsSummaries()
    {
        // Arrange
        var summaries = new List<CompanyJobSummaryResponse>
        {
            new(Guid.NewGuid(), "Corp A", 3,
            [
                new JobSummaryItem("Engineer", "NYC", "FullTime", "$100k-$150k", DateTime.UtcNow)
            ])
        };

        var json = JsonSerializer.Serialize(summaries, _jsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.ListCompanyJobSummariesAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].CompanyName.ShouldBe("Corp A");
        result.Data[0].JobCount.ShouldBe(3);
    }

    [Fact]
    public async Task ListCompanyJobSummariesAsync_WhenError_ReturnsFailure()
    {
        // Arrange
        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.InternalServerError, "Error");

        // Act
        var result = await _sut.ListCompanyJobSummariesAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
    }

    #endregion

    #region ListDrafts

    [Fact]
    public async Task ListDrafts_Success_ReturnsDraftList()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var drafts = new List<JobDraftResponse>
        {
            new()
            {
                Title = "Draft 1", AboutRole = "Role 1", Location = "Remote",
                Id = Guid.NewGuid().ToString()
            },
            new()
            {
                Title = "Draft 2", AboutRole = "Role 2", Location = "NYC",
                Id = Guid.NewGuid().ToString()
            }
        };

        _daprClient.SetupInvokeMethodWithResponse(drafts);

        // Act
        var result = await _sut.ListDrafts(companyId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        result.Data[0].Title.ShouldBe("Draft 1");
    }

    [Fact]
    public async Task ListDrafts_WhenJobApiReturnsError_ReturnsFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();

        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.BadRequest, "Bad request");

        // Act
        var result = await _sut.ListDrafts(companyId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ListDrafts_WhenExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();

        _daprClient.SetupInvokeMethodWithException(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _sut.ListDrafts(companyId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions!.Message.ShouldContain("Service unavailable");
    }

    #endregion

    #region GetDraft

    [Fact]
    public async Task GetDraft_Success_ReturnsDraft()
    {
        // Arrange
        var draftId = Guid.NewGuid();
        var draft = new JobDraftResponse
        {
            Title = "Senior Engineer",
            AboutRole = "Lead the team",
            Location = "Remote",
            Id = draftId.ToString()
        };

        _daprClient.SetupInvokeMethodWithResponse(draft);

        // Act
        var result = await _sut.GetDraft(draftId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Title.ShouldBe("Senior Engineer");
    }

    [Fact]
    public async Task GetDraft_WhenNotFound_ReturnsFailure()
    {
        // Arrange
        var draftId = Guid.NewGuid();

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"message\":\"Not found\"}", System.Text.Encoding.UTF8, "application/json")
        };
        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.GetDraft(draftId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        result.Data.ShouldBeNull();
    }

    [Fact]
    public async Task GetDraft_WhenExceptionThrown_ReturnsFailure()
    {
        // Arrange
        var draftId = Guid.NewGuid();

        _daprClient.SetupInvokeMethodWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.GetDraft(draftId, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion
}
