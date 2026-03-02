using JobBoard.Application.Actions.Applications.Submit;
using JobBoard.Application.Actions.Profiles.Get;
using JobBoard.Application.Actions.Profiles.Upsert;
using JobBoard.Application.Actions.Resumes.Delete;
using JobBoard.Application.Actions.Resumes.List;
using JobBoard.Application.Actions.Resumes.Upload;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Handles authenticated applicant operations: profile management, resume management, and job applications.
/// </summary>
[Route("api/[controller]")]
public class ApplicantController : BaseApiController
{
    /// <summary>
    /// Retrieves the authenticated user's profile data.
    /// </summary>
    /// <returns>An IActionResult containing the user's profile details wrapped in an ApiResponse if the operation is successful; otherwise, an appropriate error response.</returns>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        return await ExecuteQueryAsync(new GetUserProfileQuery(), Ok);
    }

    /// <summary>
    /// Updates or creates the authenticated user's profile based on the provided request data.
    /// </summary>
    /// <param name="request">An instance of UserProfileRequest containing the user's profile details such as phone, LinkedIn, portfolio, experience, skills, preferred location, and job type.</param>
    /// <returns>An IActionResult containing an ApiResponse that encapsulates the outcome of the operation, including the updated or created profile details if successful; otherwise, an appropriate error response.</returns>
    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile([FromBody] UserProfileRequest request)
    {
        return await ExecuteCommandAsync(new UpsertUserProfileCommand(request), Ok);
    }

    /// <summary>
    /// Uploads a resume file for the authenticated user.
    /// </summary>
    /// <param name="file">The resume file (PDF, DOCX, or TXT, max 5 MB).</param>
    /// <returns>201 Created with the resume details if successful; otherwise, an appropriate error response.</returns>
    [HttpPost("resumes")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        var command = new UploadResumeCommand(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length);

        return await ExecuteCommandAsync(command,
            result => StatusCode(StatusCodes.Status201Created, result));
    }

    /// <summary>
    /// Returns all resumes uploaded by the authenticated user.
    /// </summary>
    /// <returns>An IActionResult containing a list of resume details wrapped in an ApiResponse.</returns>
    [HttpGet("resumes")]
    public async Task<IActionResult> GetResumes()
    {
        return await ExecuteQueryAsync(new GetUserResumesQuery(), Ok);
    }

    /// <summary>
    /// Deletes a specific resume belonging to the authenticated user.
    /// </summary>
    /// <param name="id">The unique identifier of the resume to delete.</param>
    /// <returns>204 No Content if successful; 404 if the resume is not found.</returns>
    [HttpDelete("resumes/{id:guid}")]
    public async Task<IActionResult> DeleteResume(Guid id)
    {
        return await ExecuteCommandAsync(new DeleteResumeCommand(id),
            _ => NoContent());
    }

    /// <summary>
    /// Submits a job application for the authenticated user.
    /// </summary>
    /// <param name="request">The request payload containing the job ID, resume ID, and an optional cover letter.</param>
    /// <returns>An IActionResult containing the application response wrapped in an ApiResponse if the operation is successful, with a 201 Created status code; otherwise, an appropriate error response.</returns>
    [HttpPost("applications")]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationRequest request)
    {
        return await ExecuteCommandAsync(
            new SubmitApplicationCommand(request),
            result => StatusCode(StatusCodes.Status201Created, result));
    }
}
