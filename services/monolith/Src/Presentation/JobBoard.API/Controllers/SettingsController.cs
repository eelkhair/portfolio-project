using JobBoard.Application.Actions.Settings;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Settings Controller - proxies to AI Service v2
/// </summary>
public class SettingsController : BaseApiController
{
    /// <summary>
    /// Get current AI provider settings
    /// </summary>
    [HttpGet("provider")]
    public async Task<IActionResult> GetProvider()
        => await ExecuteQueryAsync(new GetProviderQuery(), Ok);

    /// <summary>
    /// Update AI provider settings
    /// </summary>
    [HttpPut("update-provider")]
    public async Task<IActionResult> UpdateProvider(UpdateProviderRequest request)
        => await ExecuteCommandAsync(new UpdateProviderCommand { Request = request }, Ok);
}
