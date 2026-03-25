using System.Security.Claims;
using Elkhair.Dev.Common.Application;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Application;

[Trait("Category", "Unit")]
public class ExtensionsTests
{
    [Fact]
    public void GetUserId_WithSubClaim_ReturnsUserId()
    {
        // Arrange
        var userId = "user-456";
        var claims = new List<Claim> { new("sub", userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetUserId_WithNoSubClaim_ReturnsNA()
    {
        // Arrange
        var claims = new List<Claim> { new("email", "test@test.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.ShouldBe("N/A");
    }

    [Fact]
    public void GetUserId_WithEmptyClaims_ReturnsNA()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.ShouldBe("N/A");
    }

    [Fact]
    public void GetUserId_WithMultipleSubClaims_ReturnsFirst()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "first-user"),
            new("sub", "second-user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.ShouldBe("first-user");
    }

    [Fact]
    public void GetUserId_WithMixedClaims_ReturnsSubValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("email", "test@test.com"),
            new("name", "Test User"),
            new("sub", "the-user-id"),
            new("role", "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.ShouldBe("the-user-id");
    }
}
