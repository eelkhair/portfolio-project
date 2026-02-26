using System.Security.Claims;
using JobBoard.API.Infrastructure;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Presentation;

[Trait("Category", "Unit")]
public class HttpUserAccessorTests
{
    [Fact]
    public void Constructor_WithAuthenticatedUser_ShouldExtractUserIdFromClaims()
    {
        var accessor = CreateAccessor(
            authenticated: true,
            claims: [new Claim(ClaimTypes.NameIdentifier, "user-123")]);

        accessor.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void Constructor_WithXUserIdHeader_ShouldPreferHeaderOverClaim()
    {
        var accessor = CreateAccessor(
            authenticated: true,
            claims: [new Claim(ClaimTypes.NameIdentifier, "claim-id")],
            headers: new Dictionary<string, string> { ["x-user-id"] = "header-id" });

        accessor.UserId.ShouldBe("header-id");
    }

    [Fact]
    public void Constructor_WithCustomClaims_ShouldExtractAllFields()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-456"),
            new Claim("https://eelkhair.net/first_name", "Jane"),
            new Claim("https://eelkhair.net/last_name", "Smith"),
            new Claim("https://eelkhair.net/email", "jane@test.com"),
            new Claim("https://eelkhair.net/roles", "Admin"),
            new Claim("https://eelkhair.net/roles", "User"),
        };

        var accessor = CreateAccessor(authenticated: true, claims: claims);

        accessor.FirstName.ShouldBe("Jane");
        accessor.LastName.ShouldBe("Smith");
        accessor.Email.ShouldBe("jane@test.com");
        accessor.Roles.ShouldBe(new List<string> { "Admin", "User" });
    }

    [Fact]
    public void Constructor_WithUnauthenticatedUser_ShouldLeavePropertiesNull()
    {
        var accessor = CreateAccessor(authenticated: false);

        accessor.UserId.ShouldBeNull();
        accessor.FirstName.ShouldBeNull();
        accessor.LastName.ShouldBeNull();
        accessor.Email.ShouldBeNull();
        accessor.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithNullHttpContext_ShouldLeavePropertiesNull()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var accessor = new HttpUserAccessor(httpContextAccessor);

        accessor.UserId.ShouldBeNull();
        accessor.FirstName.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithMissingCustomClaims_ShouldDefaultToEmpty()
    {
        var accessor = CreateAccessor(
            authenticated: true,
            claims: [new Claim(ClaimTypes.NameIdentifier, "user-789")]);

        accessor.FirstName.ShouldBe(string.Empty);
        accessor.LastName.ShouldBe(string.Empty);
        accessor.Email.ShouldBe(string.Empty);
        accessor.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithAuthorizationHeader_ShouldExtractToken()
    {
        var accessor = CreateAccessor(
            authenticated: true,
            claims: [new Claim(ClaimTypes.NameIdentifier, "user-1")],
            headers: new Dictionary<string, string> { ["Authorization"] = "Bearer test-token" });

        accessor.Token.ShouldBe("Bearer test-token");
    }

    private static HttpUserAccessor CreateAccessor(
        bool authenticated,
        Claim[]? claims = null,
        Dictionary<string, string>? headers = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticated)
        {
            var identity = new ClaimsIdentity(claims ?? [], "TestScheme");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                httpContext.Request.Headers[header.Key] = header.Value;
            }
        }

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return new HttpUserAccessor(httpContextAccessor);
    }
}
