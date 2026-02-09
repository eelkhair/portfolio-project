using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Settings;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Settings Controller
/// </summary>
public class SettingsController(ISettingsService settingsService) : BaseApiController
{
    /// <summary>
    /// Get current AI provider settings
    /// </summary>
    /// <returns></returns>
    [HttpGet("provider")]
    [AllowAnonymous]
    [StandardApiResponses]
    public async Task<IActionResult> GetProvider()
    {
        var result = await settingsService.GetProviderAsync();
        return Ok(result);
    }

    /// <summary>
    /// Update Settings for the application
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("update-provider")]
    [AllowAnonymous]
    [StandardApiResponses]
    public async Task<IActionResult> UpdateProvider(UpdateProviderRequest request)
        => await ExecuteCommandAsync(new UpdateProviderCommand(request), Ok);
}