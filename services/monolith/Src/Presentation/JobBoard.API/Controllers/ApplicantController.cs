using JobBoard.Application.Actions.Applications.List;
using JobBoard.Application.Actions.Applications.Submit;
using JobBoard.Application.Actions.Profiles.Get;
using JobBoard.Application.Actions.Profiles.Upsert;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Handles authenticated applicant operations: profile management and job applications.
/// </summary>
public class ApplicantController : BaseApiController
{
    /// <summary>
    /// Retrieves the authenticated user's profile data.
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        return await ExecuteQueryAsync(new GetUserProfileQuery(), Ok);
    }

    /// <summary>
    /// Updates or creates the authenticated user's profile based on the provided request data.
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile([FromBody] UserProfileRequest request)
    {
        return await ExecuteCommandAsync(new UpsertUserProfileCommand(request), Ok);
    }

    /// <summary>
    /// Lists all applications submitted by the authenticated user.
    /// </summary>
    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications()
    {
        return await ExecuteQueryAsync(new ListUserApplicationsQuery(), Ok);
    }

    /// <summary>
    /// Submits a job application for the authenticated user.
    /// </summary>
    [HttpPost("applications")]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationRequest request)
    {
        return await ExecuteCommandAsync(
            new SubmitApplicationCommand(request),
            result => StatusCode(StatusCodes.Status201Created, result));
    }
}
