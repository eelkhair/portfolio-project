using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;

namespace UserApi.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class KeycloakTokenStartupServiceTests
{
    private readonly IKeycloakTokenService _tokenService = Substitute.For<IKeycloakTokenService>();
    private readonly ILogger<KeycloakTokenStartupService> _logger = Substitute.For<ILogger<KeycloakTokenStartupService>>();

    [Fact]
    public async Task StartAsync_ShouldRefreshToken()
    {
        // Arrange
        _tokenService.RefreshAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns("new-token");

        var sut = new KeycloakTokenStartupService(_logger, _tokenService);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        await _tokenService.Received(1).RefreshAccessTokenAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenTokenRefreshFails_ShouldNotThrow()
    {
        // Arrange
        _tokenService.RefreshAccessTokenAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Keycloak down"));

        var sut = new KeycloakTokenStartupService(_logger, _tokenService);

        // Act & Assert - should not propagate exception
        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteImmediately()
    {
        var sut = new KeycloakTokenStartupService(_logger, _tokenService);

        // Act & Assert
        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
