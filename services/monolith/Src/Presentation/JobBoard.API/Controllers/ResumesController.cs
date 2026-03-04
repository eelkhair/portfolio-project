using System.Net;
using JobBoard.API.Helpers;
using JobBoard.API.Infrastructure.SignalR.ResumeParse;
using JobBoard.Application.Actions.Resumes.CompleteParse;
using JobBoard.Application.Actions.Resumes.Delete;
using JobBoard.Application.Actions.Resumes.Download;
using JobBoard.Application.Actions.Resumes.FailParse;
using JobBoard.Application.Actions.Resumes.GetParsedContent;
using JobBoard.Application.Actions.Resumes.List;
using JobBoard.Application.Actions.Resumes.SetDefault;
using JobBoard.Application.Actions.Resumes.Upload;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Handles resume management: upload, download, delete, listing, and AI parse callbacks.
/// </summary>
public class ResumesController(IUserAccessor accessor, IResumeParseNotifier resumeParseNotifier) : BaseApiController
{
    /// <summary>
    /// Uploads a resume file for the authenticated user. Parsing is async via outbox + AI service.
    /// </summary>
    /// <param name="file">The resume file (PDF, DOCX, or TXT, max 5 MB).</param>
    /// <param name="currentPage">The frontend route the user is currently on, propagated through the event chain for UX decisions.</param>
    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadResume(IFormFile file, [FromQuery] string? currentPage)
    {
        var command = new UploadResumeCommand(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length,
            currentPage);

        return await ExecuteCommandAsync(command,
            result => StatusCode(StatusCodes.Status201Created, result));
    }

    /// <summary>
    /// Returns all resumes uploaded by the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetResumes()
    {
        return await ExecuteQueryAsync(new GetUserResumesQuery(), Ok);
    }

    /// <summary>
    /// Returns the AI-parsed content (contact info, skills, experience) for a specific resume.
    /// </summary>
    [HttpGet("{id:guid}/parsed-content")]
    public async Task<IActionResult> GetResumeParsedContent(Guid id)
    {
        return await ExecuteQueryAsync(new GetResumeParsedContentQuery(id), Ok);
    }

    /// <summary>
    /// Internal endpoint for service-to-service retrieval of parsed resume content.
    /// Does not enforce user ownership — intended for Dapr service invocation only.
    /// </summary>
    [HttpGet("{id:guid}/parsed-content/internal")]
    [Authorize(Policy = "DaprInternal")]
    public async Task<IActionResult> GetResumeParsedContentInternal(Guid id)
    {
        return await ExecuteQueryAsync(new GetResumeParsedContentInternalQuery(id), Ok);
    }

    /// <summary>
    /// Downloads a specific resume file belonging to the authenticated user.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadResume(Guid id, [FromQuery] bool inline = false)
    {
        try
        {
            var query = new DownloadResumeQuery(id);
            var handler = HttpContext.RequestServices
                .GetRequiredService<IHandler<DownloadResumeQuery, ResumeDownloadResult>>();

            var result = await handler.HandleAsync(query, HttpContext.RequestAborted);

            if (inline)
            {
                Response.Headers.ContentDisposition = $"inline; filename=\"{result.OriginalFileName}\"";
                return File(result.Content, result.ContentType);
            }

            return File(result.Content, result.ContentType, result.OriginalFileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Fail<object>(ex.Message, HttpStatusCode.Unauthorized));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse.Fail<object>(ex.Message, HttpStatusCode.NotFound));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse.Fail<object>("An unexpected error occurred.", HttpStatusCode.InternalServerError));
        }
    }

    /// <summary>
    /// Sets a specific resume as the default for job matching.
    /// </summary>
    [HttpPatch("{id:guid}/default")]
    public async Task<IActionResult> SetDefaultResume(Guid id)
    {
        return await ExecuteCommandAsync(new SetDefaultResumeCommand(id), _ => NoContent());
    }

    /// <summary>
    /// Deletes a specific resume belonging to the authenticated user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteResume(Guid id)
    {
        return await ExecuteCommandAsync(new DeleteResumeCommand(id),
            _ => NoContent());
    }

    /// <summary>
    /// Callback from AI service after resume parsing completes successfully.
    /// </summary>
    [HttpPost("parse-completed")]
    [Authorize(Policy = "DaprInternal")]
    public async Task<IActionResult> ResumeParseCompleted([FromBody] ResumeParseCompletedModel request,
        CancellationToken cancellationToken)
    {
        accessor.UserId = request.UserId;

        await ExecuteCommandAsync(new CompleteResumeParseCommand(request), Ok);

        await resumeParseNotifier.NotifyParsedAsync(
            request.ResumeUId, request.UserId, request.CurrentPage, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Callback from AI service after resume parsing fails.
    /// </summary>
    [HttpPost("parse-failed")]
    [Authorize(Policy = "DaprInternal")]
    public async Task<IActionResult> ResumeParseFailed([FromBody] ResumeParseFailedModel request,
        CancellationToken cancellationToken)
    {
        accessor.UserId = request.UserId;

        var command = new FailResumeParseCommand(request);
        var handler = HttpContext.RequestServices
            .GetRequiredService<IHandler<FailResumeParseCommand, ResumeParseFailureResult>>();

        var failureResult = await handler.HandleAsync(command, cancellationToken);

        await resumeParseNotifier.NotifyParseFailedAsync(
            request.ResumeUId, request.UserId, request.CurrentPage,
            failureResult.Attempt, failureResult.MaxAttempts, failureResult.IsFinal,
            cancellationToken);

        return Ok();
    }
}
