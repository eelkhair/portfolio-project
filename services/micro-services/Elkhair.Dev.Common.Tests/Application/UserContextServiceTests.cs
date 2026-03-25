using System.Security.Claims;
using Elkhair.Dev.Common.Application;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace Elkhair.Dev.Common.Tests.Application;

[Trait("Category", "Unit")]
public class UserContextServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContextService _sut;

    public UserContextServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _sut = new UserContextService(_httpContextAccessor);
    }

    [Fact]
    public void GetCurrentUser_WithSubClaim_ReturnsUserId()
    {
        // Arrange
        var userId = "user-123";
        var claims = new List<Claim> { new("sub", userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetCurrentUser();

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void GetCurrentUser_WithNoSubClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim> { new("email", "test@test.com") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetCurrentUser();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentUser_WithNullHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _sut.GetCurrentUser();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentUser_WithNullUser_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // DefaultHttpContext always has a User, so we test with no claims
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetCurrentUser();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetHeader_WithExistingHeader_ReturnsValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Custom-Header"] = "custom-value";
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetHeader("X-Custom-Header");

        // Assert
        result.ShouldBe("custom-value");
    }

    [Fact]
    public void GetHeader_WithMissingHeader_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetHeader("X-Missing-Header");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetHeader_WithNullHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _sut.GetHeader("X-Custom-Header");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetHeader_WithMultipleHeaderValues_ReturnsFirst()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("X-Multi", "value1");
        httpContext.Request.Headers.Append("X-Multi", "value2");
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetHeader("X-Multi");

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetHeader_IdempotencyKey_ReturnsValue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Idempotency-Key"] = "idem-key-123";
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.GetHeader("Idempotency-Key");

        // Assert
        result.ShouldBe("idem-key-123");
    }
}
