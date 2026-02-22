using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Settings;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Actions.Settings.Provider; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Settings Controller
/// </summary>
public class SettingsController : BaseApiController
{
    /// <summary>
    /// Get current AI provider settings
    /// </summary>
    /// <returns></returns>
    [HttpGet("provider")]
    [StandardApiResponses]
    public async Task<IActionResult> GetProvider() 
        => await ExecuteQueryAsync(new GetProviderQuery(), Ok);
    

    /// <summary>
    /// Update Settings for the application
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("update-provider")]
    [StandardApiResponses]
    public async Task<IActionResult> UpdateProvider(UpdateProviderRequest request)
        => await ExecuteCommandAsync(new UpdateProviderCommand(request), Ok);
    
    /// <summary>
    /// Get current application mode
    /// </summary>
    /// <returns></returns>
    [HttpGet("mode")]
    [StandardApiResponses]
    public async Task<IActionResult> GetApplicationMode() 
        => await ExecuteQueryAsync(new GetApplicationModeQuery(), Ok);
    
    /// <summary>
    /// Set application mode
    /// <param name="request"></param>
    /// </summary>
    /// <returns></returns>
    [HttpPut("mode")]
    [StandardApiResponses]
    public async Task<IActionResult> UpdateApplicationMode(ApplicationModeDto request)
        => await ExecuteCommandAsync(new UpdateApplicationModeCommand(request), Ok);
}