using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdminApi.Application.Commands;
using AdminApi.Tests.Helpers;
using AdminAPI.Contracts.Models.Settings;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace AdminApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class SettingsCommandServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly ILogger<SettingsCommandService> _logger = Substitute.For<ILogger<SettingsCommandService>>();
    private readonly SettingsCommandService _sut;

    public SettingsCommandServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new SettingsCommandService(_daprClient, _accessor, _logger);
    }

    #region GetProvider

    [Fact]
    public async Task GetProviderAsync_Success_ReturnsProvider()
    {
        // Arrange
        var providerResponse = new ApiResponse<GetProviderResponse>
        {
            Data = new GetProviderResponse { Provider = "openai", Model = "gpt-4.1-mini" },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        _daprClient.SetupInvokeMethodWithResponse(providerResponse);

        // Act
        var result = await _sut.GetProviderAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Provider.ShouldBe("openai");
        result.Data.Model.ShouldBe("gpt-4.1-mini");
    }

    [Fact]
    public async Task GetProviderAsync_WhenAiServiceReturnsError_ReturnsFailure()
    {
        // Arrange
        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.ServiceUnavailable, "Service down");

        // Act
        var result = await _sut.GetProviderAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProviderAsync_WhenExceptionThrown_ReturnsFailure()
    {
        // Arrange
        _daprClient.SetupInvokeMethodWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _sut.GetProviderAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions!.Message.ShouldContain("Connection refused");
    }

    #endregion

    #region UpdateProvider

    [Fact]
    public async Task UpdateProviderAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new UpdateProviderRequest { Provider = "claude", Model = "claude-sonnet-4-20250514" };

        var json = JsonSerializer.Serialize(new { success = true }, JsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.UpdateProviderAsync(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateProviderAsync_WhenAiServiceReturnsError_ReturnsFailure()
    {
        // Arrange
        var request = new UpdateProviderRequest { Provider = "invalid", Model = "invalid-model" };

        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.BadRequest, "Invalid provider");

        // Act
        var result = await _sut.UpdateProviderAsync(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GetApplicationMode

    [Fact]
    public async Task GetApplicationModeAsync_Success_ReturnsMode()
    {
        // Arrange
        var modeResponse = new ApiResponse<ApplicationModeDto>
        {
            Data = new ApplicationModeDto { IsMonolith = true },
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        _daprClient.SetupInvokeMethodWithResponse(modeResponse);

        // Act
        var result = await _sut.GetApplicationModeAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.IsMonolith.ShouldBeTrue();
    }

    [Fact]
    public async Task GetApplicationModeAsync_WhenError_ReturnsFailure()
    {
        // Arrange
        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.InternalServerError, "Error");

        // Act
        var result = await _sut.GetApplicationModeAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region UpdateApplicationMode

    [Fact]
    public async Task UpdateApplicationModeAsync_Success_ReturnsUpdatedMode()
    {
        // Arrange
        var request = new ApplicationModeDto { IsMonolith = false };

        var json = JsonSerializer.Serialize(new { success = true }, JsonOpts);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        _daprClient.InvokeMethodWithResponseAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var result = await _sut.UpdateApplicationModeAsync(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.IsMonolith.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateApplicationModeAsync_WhenError_ReturnsFailure()
    {
        // Arrange
        var request = new ApplicationModeDto { IsMonolith = true };

        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.InternalServerError, "Service error");

        // Act
        var result = await _sut.UpdateApplicationModeAsync(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region ReEmbedJobs

    [Fact]
    public async Task ReEmbedJobsAsync_Success_ReturnsJobsProcessedCount()
    {
        // Arrange
        var reEmbedResponse = new ApiResponse<ReEmbedJobsResponse>
        {
            Data = new ReEmbedJobsResponse(42),
            Success = true,
            StatusCode = HttpStatusCode.OK
        };

        _daprClient.SetupInvokeMethodWithResponse(reEmbedResponse);

        // Act
        var result = await _sut.ReEmbedJobsAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.JobsProcessed.ShouldBe(42);
    }

    [Fact]
    public async Task ReEmbedJobsAsync_WhenError_ReturnsFailure()
    {
        // Arrange
        _daprClient.SetupInvokeMethodWithErrorResponse(HttpStatusCode.InternalServerError, "Embedding failed");

        // Act
        var result = await _sut.ReEmbedJobsAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Exceptions.ShouldNotBeNull();
    }

    #endregion
}
