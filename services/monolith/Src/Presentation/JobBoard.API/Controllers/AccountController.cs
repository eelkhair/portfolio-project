using JobBoard.API.Helpers;
using JobBoard.Application.Actions.Account.Signup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
