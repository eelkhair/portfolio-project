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
        UserId = user.FindFirstValue(ClaimConstants.NameIdentifierId);
        FirstName = user.FindFirstValue( "https://eelkhair.net/first_name")?? string.Empty;
        LastName = user.FindFirstValue("https://eelkhair.net/last_name")?? string.Empty;
        Email = user.FindFirstValue( "https://eelkhair.net/email")?? string.Empty;
        Roles = user.FindAll("https://eelkhair.net/roles").Select(c => c.Value).ToList();
    }
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// Gets the first name.
    /// </summary>
    public string? FirstName { get; set; } 
    /// <summary>
    /// Gets the last name.
    /// </summary>
    public string? LastName { get; set; }
    /// <summary>
    /// Gets the email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets the roles.
    /// </summary>
    public List<string> Roles { get; set; } = [];
}