using AdminApi.Application.Queries;
using AdminApi.Tests.Helpers;
using CompanyAPI.Contracts.Models.Industries.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace AdminApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class IndustryQueryServiceTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly IndustryQueryService _sut;

    public IndustryQueryServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new IndustryQueryService(_daprClient, _accessor);
    }

    [Fact]
    public async Task ListAsync_Success_ReturnsIndustryList()
    {
        // Arrange
        var industries = new List<IndustryResponse>
        {
            new() { UId = Guid.NewGuid(), Name = "Technology", CreatedAt = DateTime.UtcNow },
            new() { UId = Guid.NewGuid(), Name = "Healthcare", CreatedAt = DateTime.UtcNow }
        };

        _daprClient.InvokeMethodAsync<List<IndustryResponse>>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(industries);

        // Act
        var result = await _sut.ListAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        result.Data[0].Name.ShouldBe("Technology");
        result.Data[1].Name.ShouldBe("Healthcare");
    }

    [Fact]
    public async Task ListAsync_WhenDaprThrowsException_Propagates()
    {
        // Arrange - Non-InvocationException is not caught by DaprExtensions.Process
        _daprClient.InvokeMethodAsync<List<IndustryResponse>>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act & Assert - Exception propagates since DaprExtensions.Process only catches InvocationException
        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenNoIndustries()
    {
        // Arrange
        _daprClient.InvokeMethodAsync<List<IndustryResponse>>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(new List<IndustryResponse>());

        // Act
        var result = await _sut.ListAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(0);
    }
}
