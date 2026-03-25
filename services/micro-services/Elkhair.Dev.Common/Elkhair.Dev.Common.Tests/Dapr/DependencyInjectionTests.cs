using Elkhair.Dev.Common.Application;
using Elkhair.Dev.Common.Dapr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Dapr;

[Trait("Category", "Unit")]
public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = Substitute.For<IConfiguration>();

        // Act & Assert
        Should.NotThrow(() => services.AddInfrastructure(configuration));
    }

    [Fact]
    public void AddMessageSender_RegistersIMessageSender()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Act
        services.AddMessageSender();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageSender));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddMessageSender_RegistersUserContextService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Act
        services.AddMessageSender();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(UserContextService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddStateManager_RegistersIStateManager()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddStateManager();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IStateManager));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }
}
