using JobBoard.API.Helpers;
using JobBoard.Application.Actions.Account.Signup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobBoard.API.Controllers;

/// <summary>
/// Self-signup endpoints. All routes are anonymous; bot/abuse protection is via Cloudflare Turnstile
/// (token required in each command). Subsequent logins go through Keycloak as usual.
/// </summary>
[AllowAnonymous]
public class AccountController : BaseApiController
{
    [HttpPost("signup/admin")]
    [ProducesResponseType(typeof(ApiResponse<SignupResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignupAdmin([FromBody] SignupAdminCommand command)
    {
        command.RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        return await ExecuteCommandAsync(command,
            result => StatusCode(StatusCodes.Status201Created, result));
    }

    [HttpPost("signup/public")]
    [ProducesResponseType(typeof(ApiResponse<SignupResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignupPublic([FromBody] SignupPublicCommand command)
    {
        command.RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        return await ExecuteCommandAsync(command,
            result => StatusCode(StatusCodes.Status201Created, result));
    }

    /// <summary>
    /// One-click guest signup for the public app. Called from the Keycloak login page JS —
    /// returns random credentials which the JS types into the login form and submits.
    /// </summary>
    [HttpPost("signup/public/anonymous")]
    [EnableRateLimiting("anonymous-signup")]
    [ProducesResponseType(typeof(ApiResponse<AnonymousSignupResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> SignupPublicAnonymous() => CreateAnonymousAsync("/Applicants");

    /// <summary>
    /// One-click guest signup for the admin app. Same flow as the public variant but lands in /Admins.
    /// </summary>
    [HttpPost("signup/admin/anonymous")]
    [EnableRateLimiting("anonymous-signup")]
    [ProducesResponseType(typeof(ApiResponse<AnonymousSignupResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> SignupAdminAnonymous() => CreateAnonymousAsync("/Admins");

    private Task<IActionResult> CreateAnonymousAsync(string groupPath)
    {
        var command = new SignupAnonymousCommand
        {
            GroupPath = groupPath,
            RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        return ExecuteCommandAsync(command,
            result => StatusCode(StatusCodes.Status201Created, result));
    }
}
