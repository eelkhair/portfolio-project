using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Drafts Controller 
/// </summary>
public class DraftsController : BaseApiController
{
    /// <summary>
    /// Generate a job draft using AI
    /// </summary>
    /// <param name="companyId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("{companyId:guid}/generate")]
    [AllowAnonymous]
    [StandardApiResponses]
   
    public async Task<IActionResult> Generate(Guid companyId, DraftGenRequest request)
        => await ExecuteCommandAsync(new DraftGenCommand(companyId, request), Ok);
    
}