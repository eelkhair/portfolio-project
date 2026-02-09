using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Settings Controller 
/// </summary>
public class SettingsController : BaseApiController
{
    /// <summary>
    /// Update Settings for the application
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("update-provider")]
    [AllowAnonymous]
    [StandardApiResponses]
   
    public async Task<IActionResult> Generate(UpdateProviderRequest request)
        => await ExecuteCommandAsync(new UpdateProviderCommand(request), Ok);
    
}