using System.Security.Claims;
using Elkhair.Dev.Common.Application;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace AdminApi.Tests.Helpers;

/// <summary>
/// Creates a UserContextService backed by a faked IHttpContextAccessor
/// with configurable user ID and Authorization header.
/// </summary>
public static class FakeUserContextService
{
    public static UserContextService Create(
        string userId = "test-user-123",
        string authHeader = "Bearer test-token")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId)
        ], "TestAuth"));
        httpContext.Request.Headers["Authorization"] = authHeader;

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new UserContextService(accessor);
    }
}
