using System.Net;
using AdminApi.Application.Commands;
using AdminApi.Tests.Helpers;
using AdminAPI.Contracts.Models.Jobs;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace AdminApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class OpenAICommandServiceTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly ILogger<OpenAICommandService> _logger = Substitute.For<ILogger<OpenAICommandService>>();
    private readonly OpenAICommandService _sut;

    public OpenAICommandServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new OpenAICommandService(_daprClient, _accessor, _logger);
    }

    [Fact]
    public async Task GenerateJobAsync_Success_ReturnsJobGenResponse()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobGenRequest
        {
            Brief = "Senior backend engineer for payments team",
            RoleLevel = RoleLevel.Senior,
            Tone = Tone.Neutral,
            MaxBullets = 6,
            CompanyName = "Test Corp",
            Location = "Remote"
        };

        var genResponse = new ApiResponse<JobGenResponse>
        {
            Data = new JobGenResponse
            {
                Title = "Senior Backend Engineer",
                AboutRole = "Lead backend development for payments infrastructure",
                Responsibilities = ["Design APIs", "Mentor juniors"],
                Qualifications = ["5+ years experience", "C# expertise"],
                Location = "Remote",
                DraftId = Guid.NewGuid().ToString()
            },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        _daprClient.SetupInvokeMethodWithResponse(genResponse);

        // Act
        var result = await _sut.GenerateJobAsync(companyId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Title.ShouldBe("Senior Backend Engineer");
        result.Data.Responsibilities.Count.ShouldBe(2);
        result.Data.Qualifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GenerateJobAsync_CallsAiServiceViaDapr()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobGenRequest { Brief = "Test brief" };

        var genResponse = new ApiResponse<JobGenResponse>
        {
            Data = new JobGenResponse { Title = "Test" },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        _daprClient.SetupInvokeMethodWithResponse(genResponse);

        // Act
        await _sut.GenerateJobAsync(companyId, request, CancellationToken.None);

        // Assert - Verify CreateInvokeMethodRequest was called with ai-service-v2
        _daprClient.Received(1).CreateInvokeMethodRequest(
            HttpMethod.Post,
            "ai-service-v2",
            $"drafts/{companyId}/generate");
    }

    [Fact]
    public async Task GenerateJobAsync_WhenAiServiceReturnsError_ReturnsFailure()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobGenRequest { Brief = "Test brief" };

        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.InternalServerError, "AI service error");

        // Act
        var result = await _sut.GenerateJobAsync(companyId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions.ShouldNotBeNull();
    }

    [Fact]
    public async Task GenerateJobAsync_WhenExceptionThrown_ReturnsFailureWithMessage()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobGenRequest { Brief = "Test brief" };

        _daprClient.SetupInvokeMethodWithException(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.GenerateJobAsync(companyId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions!.Message.ShouldContain("Network error");
    }

    [Fact]
    public async Task GenerateJobAsync_IncludesAuthorizationHeader()
    {
        // Arrange
        var companyId = Guid.NewGuid().ToString();
        var request = new JobGenRequest { Brief = "Test brief" };

        var genResponse = new ApiResponse<JobGenResponse>
        {
            Data = new JobGenResponse { Title = "Test" },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        HttpRequestMessage? capturedRequest = null;
        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedRequest = callInfo.ArgAt<HttpRequestMessage>(0);
                var json = System.Text.Json.JsonSerializer.Serialize(genResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

        // Act
        await _sut.GenerateJobAsync(companyId, request, CancellationToken.None);

        // Assert - Verify auth header was added to the request
        capturedRequest.ShouldNotBeNull();
        capturedRequest!.Headers.Contains("Authorization").ShouldBeTrue();
    }
}
