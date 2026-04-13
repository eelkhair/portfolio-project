using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using UserApi.Infrastructure.Keycloak;
using UserApi.Infrastructure.Keycloak.Interfaces;

namespace UserApi.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class KeycloakFactoryTests
{
    private readonly IKeycloakTokenService _tokenService = Substitute.For<IKeycloakTokenService>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();

    [Fact]
    public async Task GetKeycloakResourceAsync_ShouldReturnResourceWithToken()
    {
        // Arrange
        _tokenService.GetAccessTokenAsync(Arg.Any<CancellationToken>())
            .Returns("test-token-123");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
(StringComparer.Ordinal)
            {
                ["Keycloak:Authority"] = "https://auth.test.com/realms/test-realm"
            })
            .Build();

        _httpClientFactory.CreateClient("keycloak").Returns(new HttpClient());

        var factory = new DefaultKeycloakFactory(_tokenService, config, _httpClientFactory);

        // Act
        var resource = await factory.GetKeycloakResourceAsync(CancellationToken.None);

        // Assert
        resource.ShouldNotBeNull();
        resource.ShouldBeOfType<KeycloakResource>();
        await _tokenService.Received(1).GetAccessTokenAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetKeycloakResourceAsync_WhenAuthorityMissing_ShouldThrow()
    {
        // Arrange
        _tokenService.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal))
            .Build();

        _httpClientFactory.CreateClient("keycloak").Returns(new HttpClient());

        var factory = new DefaultKeycloakFactory(_tokenService, config, _httpClientFactory);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => factory.GetKeycloakResourceAsync(CancellationToken.None));
    }
}
