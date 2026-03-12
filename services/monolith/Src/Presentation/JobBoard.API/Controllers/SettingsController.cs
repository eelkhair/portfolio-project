using JobBoard.Application.Actions.Settings.ApplicationMode;
using JobBoard.Application.Actions.Settings.Provider;
using JobBoard.Application.Actions.Settings.ReEmbedJobs;
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

    /// <summary>
    /// Get current application mode (monolith or microservices)
    /// </summary>
    [HttpGet("mode")]
    public async Task<IActionResult> GetApplicationMode()
        => await ExecuteQueryAsync(new GetApplicationModeQuery(), Ok);

    /// <summary>
    /// Update application mode
    /// </summary>
    [HttpPut("mode")]
    public async Task<IActionResult> UpdateApplicationMode(ApplicationModeDto request)
        => await ExecuteCommandAsync(new UpdateApplicationModeCommand(request), Ok);

    /// <summary>
    /// Re-embed all jobs — triggers AI service to regenerate all job embeddings
    /// </summary>
    [HttpPost("re-embed-jobs")]
    public async Task<IActionResult> ReEmbedAllJobs()
        => await ExecuteCommandAsync(new ReEmbedAllJobsCommand(), Ok);
}
