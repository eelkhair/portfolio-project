using AdminApi.Application.Queries;
using AdminApi.Tests.Helpers;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace AdminApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class CompanyQueryServiceTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly CompanyQueryService _sut;

    public CompanyQueryServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new CompanyQueryService(_daprClient, _accessor, Substitute.For<ILogger<CompanyQueryService>>());
    }

    [Fact]
    public async Task ListAsync_Success_ReturnsCompanyList()
    {
        // Arrange
        var companies = new List<CompanyResponse>
        {
            new() { Name = "Company A", Email = "a@test.com", UId = Guid.NewGuid() },
            new() { Name = "Company B", Email = "b@test.com", UId = Guid.NewGuid() }
        };

        _daprClient.InvokeMethodAsync<List<CompanyResponse>>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(companies);

        // Act
        var result = await _sut.ListAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        result.Data[0].Name.ShouldBe("Company A");
        result.Data[1].Name.ShouldBe("Company B");
    }

    [Fact]
    public async Task ListAsync_WhenDaprThrowsException_Propagates()
    {
        // Arrange - Non-InvocationException is not caught by DaprExtensions.Process
        _daprClient.InvokeMethodAsync<List<CompanyResponse>>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act & Assert - Exception propagates since DaprExtensions.Process only catches InvocationException
        await Should.ThrowAsync<HttpRequestException>(() =>
            _sut.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyList_WhenNoCompanies()
    {
        // Arrange
        _daprClient.InvokeMethodAsync<List<CompanyResponse>>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(new List<CompanyResponse>());

        // Act
        var result = await _sut.ListAsync(CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(0);
    }
}
