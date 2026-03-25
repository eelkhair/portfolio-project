using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands;
using AdminApi.Tests.Helpers;
using AdminAPI.Contracts.Models.Jobs.Events;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using JobAPI.Contracts.Models.Jobs.Responses;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

using ResponseJobType = JobAPI.Contracts.Models.Jobs.Responses.JobType;
using RequestJobType = JobAPI.Contracts.Enums.JobType;

namespace AdminApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class JobCommandServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly IMessageSender _messageSender = Substitute.For<IMessageSender>();
    private readonly ILogger<JobCommandService> _logger = Substitute.For<ILogger<JobCommandService>>();
    private readonly JobCommandService _sut;

    public JobCommandServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new JobCommandService(_daprClient, _accessor, _messageSender, _logger);
    }

    #region CreateDraft

    [Fact]
    public async Task CreateDraft_Success_ReturnsJobDraftResponse()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobDraftRequest
        {
            Title = "Senior Engineer",
            AboutRole = "Lead engineering efforts",
            Location = "Remote"
        };

        var draftResponse = new JobDraftResponse
        {
            Title = "Senior Engineer",
            AboutRole = "Lead engineering efforts",
            Location = "Remote",
            Id = Guid.NewGuid().ToString()
        };

        _daprClient.SetupInvokeMethodWithResponse(draftResponse);

        // Act
        var result = await _sut.CreateDraft(companyId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Title.ShouldBe("Senior Engineer");
    }

    [Fact]
    public async Task CreateDraft_WhenJobApiReturnsError_ReturnsFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobDraftRequest { Title = "Test" };

        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.BadRequest, "Validation failed");

        // Act
        var result = await _sut.CreateDraft(companyId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CreateDraft_WhenExceptionThrown_ReturnsFailureWithMessage()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobDraftRequest { Title = "Test" };

        _daprClient.SetupInvokeMethodWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.CreateDraft(companyId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions.ShouldNotBeNull();
        result.Exceptions!.Message.ShouldContain("Connection refused");
    }

    #endregion

    #region RewriteItem

    [Fact]
    public async Task RewriteItem_Success_ReturnsRewriteResponse()
    {
        // Arrange
        var request = new JobRewriteRequest
        {
            Field = "aboutRole",
            Value = "Original about role text",
            Context = new Dictionary<string, object> { { "title", "Engineer" } },
            Style = new Dictionary<string, object> { { "tone", "professional" } }
        };

        var rewriteResponse = new ApiResponse<JobRewriteResponse>
        {
            Data = new JobRewriteResponse
            {
                Field = "aboutRole",
                Options = ["Rewritten option 1", "Rewritten option 2"]
            },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        _daprClient.SetupInvokeMethodWithResponse(rewriteResponse);

        // Act
        var result = await _sut.RewriteItem(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Field.ShouldBe("aboutRole");
        result.Data.Options.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RewriteItem_WhenAiServiceReturnsError_ReturnsFailure()
    {
        // Arrange
        var request = new JobRewriteRequest
        {
            Field = "aboutRole",
            Value = "Text",
            Context = new Dictionary<string, object>(),
            Style = new Dictionary<string, object>()
        };

        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.InternalServerError, "AI service error");

        // Act
        var result = await _sut.RewriteItem(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region CreateJob

    [Fact]
    public async Task CreateJob_Success_ReturnsJobResponseAndPublishesEvent()
    {
        // Arrange
        var jobUId = Guid.NewGuid();
        var companyUId = Guid.NewGuid();
        var request = new JobCreateRequest
        {
            Title = "Software Engineer",
            CompanyUId = companyUId,
            Location = "New York",
            JobType = RequestJobType.FullTime,
            AboutRole = "Build software",
            DraftId = Guid.NewGuid().ToString(),
            DeleteDraft = true
        };

        var jobResponse = new JobResponse
        {
            UId = jobUId,
            Title = "Software Engineer",
            CompanyUId = companyUId,
            CompanyName = "Test Corp",
            Location = "New York",
            JobType = ResponseJobType.FullTime,
            AboutRole = "Build software",
            CreatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(jobResponse, JsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.CreateJob(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Title.ShouldBe("Software Engineer");
    }

    [Fact]
    public async Task CreateJob_Success_PublishesJobPublishedEvent()
    {
        // Arrange
        var jobUId = Guid.NewGuid();
        var companyUId = Guid.NewGuid();
        var draftId = Guid.NewGuid().ToString();
        var request = new JobCreateRequest
        {
            Title = "Software Engineer",
            CompanyUId = companyUId,
            Location = "New York",
            JobType = RequestJobType.FullTime,
            AboutRole = "Build software",
            DraftId = draftId,
            DeleteDraft = true
        };

        var jobResponse = new JobResponse
        {
            UId = jobUId,
            Title = "Software Engineer",
            CompanyUId = companyUId,
            CompanyName = "Test Corp",
            Location = "New York",
            JobType = ResponseJobType.FullTime,
            AboutRole = "Build software",
            CreatedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(jobResponse, JsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        await _sut.CreateJob(request, CancellationToken.None);

        // Assert
        await _messageSender.Received(1).SendEventAsync(
            PubSubNames.RabbitMq,
            "job.published.v2",
            Arg.Any<string>(),
            Arg.Is<JobPublishedEvent>(e =>
                e.UId == jobUId &&
                e.Title == "Software Engineer" &&
                e.CompanyUId == companyUId &&
                e.DraftId == draftId &&
                e.DeleteDraft),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateJob_WhenJobApiReturnsError_ReturnsFailureAndDoesNotPublish()
    {
        // Arrange
        var request = new JobCreateRequest
        {
            Title = "Software Engineer",
            CompanyUId = Guid.NewGuid(),
            Location = "New York",
            JobType = RequestJobType.FullTime,
            AboutRole = "Build software",
            DraftId = Guid.NewGuid().ToString()
        };

        var errorJson = JsonSerializer.Serialize(new ApiError { Message = "Bad request" }, JsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorJson, System.Text.Encoding.UTF8, "application/json")
        };

        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.CreateJob(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await _messageSender.DidNotReceive().SendEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<JobPublishedEvent>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteDraft

    [Fact]
    public async Task DeleteDraft_Success_CallsJobApiViaDapr()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var draftId = Guid.NewGuid();

        _daprClient.InvokeMethodAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act & Assert - should not throw
        await Should.NotThrowAsync(() =>
            _sut.DeleteDraft(companyId, draftId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteDraft_WhenExceptionThrown_Rethrows()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var draftId = Guid.NewGuid();

        _daprClient.When(x => x.InvokeMethodAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new HttpRequestException("Service unavailable"));

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.DeleteDraft(companyId, draftId, CancellationToken.None));
    }

    #endregion
}
