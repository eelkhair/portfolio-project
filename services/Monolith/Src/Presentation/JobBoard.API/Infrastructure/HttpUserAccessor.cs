using System.Security.Claims;
using JobBoard.Application.Interfaces.Users;
using Microsoft.Identity.Web;

namespace JobBoard.API.Infrastructure;

/// <summary>
/// HTTP User Accessor
/// </summary>
public class HttpUserAccessor : IUserAccessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpUserAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public HttpUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;
        UserId = user.FindFirstValue(ClaimConstants.ObjectId);
        FirstName = user.FindFirstValue(ClaimTypes.GivenName)?? string.Empty;
        LastName = user.FindFirstValue(ClaimTypes.Surname)?? string.Empty;
        Email = user.FindFirstValue(ClaimTypes.Name)?? string.Empty;
        Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    }
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public string? UserId { get; }
    /// <summary>
    /// Gets the first name.
    /// </summary>
    public string? FirstName { get; } 
    /// <summary>
    /// Gets the last name.
    /// </summary>
    public string? LastName { get; }
    /// <summary>
    /// Gets the email.
    /// </summary>
    public string? Email { get; }

    /// <summary>
    /// Gets the roles.
    /// </summary>
    public List<string> Roles { get; } = [];
}