using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Jobs.ReEmbedAll;
using JobBoard.AI.Application.Actions.Resumes.MatchExplanations;
using JobBoard.AI.Application.Actions.Settings.ApplicationMode;
using JobBoard.AI.Application.Actions.Settings.FeatureFlags;
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
    [Authorize(Policy = "SystemAdmin")]
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
    [Authorize(Policy = "SystemAdmin")]
    [StandardApiResponses]
    public async Task<IActionResult> UpdateApplicationMode(ApplicationModeDto request)
        => await ExecuteCommandAsync(new UpdateApplicationModeCommand(request), Ok);

    /// <summary>
    /// Re-embed all jobs — fetches all jobs from monolith and regenerates embeddings
    /// </summary>
    [HttpPost("re-embed-jobs")]
    [Authorize(Policy = "SystemAdmin")]
    [StandardApiResponses]
    public async Task<IActionResult> ReEmbedAllJobs()
        => await ExecuteCommandAsync(new ReEmbedAllJobsCommand(), Ok);

    /// <summary>
    /// Generate match explanations for all existing resume embeddings.
    /// Pre-computes "why this job matches" explanations using LLM for each resume's top matching jobs.
    /// </summary>
    [HttpPost("generate-match-explanations")]
    [Authorize(Policy = "SystemAdmin")]
    [StandardApiResponses]
    public async Task<IActionResult> GenerateAllMatchExplanations()
        => await ExecuteCommandAsync(new GenerateAllMatchExplanationsCommand(), Ok);

    /// <summary>
    /// Get all feature flags
    /// </summary>
    [HttpGet("feature-flags")]
    [Authorize(Policy = "SystemAdmin")]
    [StandardApiResponses]
    public async Task<IActionResult> GetFeatureFlags()
        => await ExecuteQueryAsync(new GetFeatureFlagsQuery(), Ok);

    /// <summary>
    /// Update a feature flag
    /// </summary>
    [HttpPut("feature-flags")]
    [Authorize(Policy = "SystemAdmin")]
    [StandardApiResponses]
    public async Task<IActionResult> UpdateFeatureFlag(UpdateFeatureFlagRequest request)
        => await ExecuteCommandAsync(new UpdateFeatureFlagCommand(request), Ok);
}
