using System.Net;
using AdminApi.Application.Commands;
using AdminApi.Tests.Helpers;
using AdminAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using Dapr.Client;
using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Elkhair.Dev.Common.Domain.Constants;
using JobBoard.IntegrationEvents.Company;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using UserAPI.Contracts.Models.Events;

namespace AdminApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class CompanyCommandServiceTests
{
    private readonly DaprClient _daprClient = Substitute.For<DaprClient>();
    private readonly UserContextService _accessor = FakeUserContextService.Create();
    private readonly IMessageSender _messageSender = Substitute.For<IMessageSender>();
    private readonly ILogger<CompanyCommandService> _logger = Substitute.For<ILogger<CompanyCommandService>>();
    private readonly CompanyCommandService _sut;

    public CompanyCommandServiceTests()
    {
        _daprClient.SetupCreateInvokeMethodRequest();
        _sut = new CompanyCommandService(_daprClient, _accessor, _messageSender, _logger);
    }

    [Fact]
    public async Task CreateAsync_Success_CallsCompanyApiViaDapr()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "info@testcorp.com",
            CompanyWebsite = "https://testcorp.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@testcorp.com"
        };

        var companyResponse = new CompanyResponse
        {
            Name = "Test Corp",
            Email = "info@testcorp.com",
            UId = companyUId
        };

        _daprClient.InvokeMethodAsync<CompanyResponse>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(companyResponse);

        // Act
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Name.ShouldBe("Test Corp");
    }

    [Fact]
    public async Task CreateAsync_Success_PublishesProvisionUserEvent()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "info@testcorp.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@testcorp.com"
        };

        _daprClient.InvokeMethodAsync<CompanyResponse>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyResponse { Name = "Test Corp", Email = "info@testcorp.com", UId = companyUId });

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await _messageSender.Received(1).SendEventAsync(
            PubSubNames.RabbitMq,
            "company.created",
            Arg.Any<string>(),
            Arg.Is<ProvisionUserEvent>(e =>
                e.CompanyName == "Test Corp" &&
                e.FirstName == "John" &&
                e.LastName == "Doe" &&
                e.Email == "john@testcorp.com" &&
                e.CompanyUId == companyUId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Success_PublishesMicroCompanyCreatedEvent()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        var industryUId = Guid.NewGuid();
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "info@testcorp.com",
            CompanyWebsite = "https://testcorp.com",
            IndustryUId = industryUId,
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@testcorp.com"
        };

        _daprClient.InvokeMethodAsync<CompanyResponse>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyResponse { Name = "Test Corp", Email = "info@testcorp.com", UId = companyUId });

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await _messageSender.Received(1).SendEventAsync(
            PubSubNames.RabbitMq,
            "micro.company-created.v1",
            Arg.Any<string>(),
            Arg.Is<MicroCompanyCreatedV1Event>(e =>
                e.CompanyUId == companyUId &&
                e.Name == "Test Corp" &&
                e.IndustryUId == industryUId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithExplicitUserId_UsesProvidedUserId()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "info@testcorp.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@testcorp.com",
            UserId = "explicit-user-id"
        };

        _daprClient.InvokeMethodAsync<CompanyResponse>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyResponse { Name = "Test Corp", Email = "info@testcorp.com", UId = Guid.NewGuid() });

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await _messageSender.Received(1).SendEventAsync(
            PubSubNames.RabbitMq,
            "company.created",
            "explicit-user-id",
            Arg.Any<ProvisionUserEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDaprReturnsNullData_DoesNotPublishEvents()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Test Corp",
            CompanyEmail = "info@testcorp.com",
            IndustryUId = Guid.NewGuid(),
            AdminFirstName = "John",
            AdminLastName = "Doe",
            AdminEmail = "john@testcorp.com"
        };

        // When InvokeMethodAsync returns null, DaprExtensions.Process wraps it as success with null Data
        // The code checks company.Success && company.Data is { } so events won't be published
        _daprClient.InvokeMethodAsync<CompanyResponse>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns((CompanyResponse)null!);

        // Act
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert - Data is null, so events should not be published
        await _messageSender.DidNotReceive().SendEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<ProvisionUserEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_Success_CallsCompanyApiViaDapr()
    {
        // Arrange
        var companyUId = Guid.NewGuid();
        var request = new UpdateCompanyRequest
        {
            Name = "Updated Corp",
            CompanyEmail = "updated@corp.com",
            IndustryUId = Guid.NewGuid()
        };

        _daprClient.InvokeMethodAsync<CompanyResponse>(
                Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(new CompanyResponse { Name = "Updated Corp", Email = "updated@corp.com", UId = companyUId });

        // Act
        var result = await _sut.UpdateAsync(companyUId, request, CancellationToken.None);

        // Assert
        result.Success.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Name.ShouldBe("Updated Corp");
    }
}
