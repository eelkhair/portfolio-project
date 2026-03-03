using JobBoard.AI.Application.Actions.Resumes.Parse;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Resume parsing endpoints
/// </summary>
public class ResumesController : BaseApiController
{
    /// <summary>
    /// Parse a resume file and extract structured content
    /// </summary>
    [HttpPost("parse")]
    public async Task<IActionResult> Parse([FromBody] ResumeParseRequest request)
        => await ExecuteCommandAsync(new ParseResumeCommand(request), Ok);
}
