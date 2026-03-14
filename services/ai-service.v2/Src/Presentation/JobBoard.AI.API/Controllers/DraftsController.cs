using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Drafts.Generate;
using JobBoard.AI.Application.Actions.Drafts.RewriteItem;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Drafts Controller — LLM-powered operations only (generate + rewrite)
/// </summary>
public class DraftsController : BaseApiController
{
    /// <summary>
    /// Generate a job draft using AI
    /// </summary>
    [HttpPost("{companyId:guid}/generate")]
    [StandardApiResponses]
    public async Task<IActionResult> Generate(Guid companyId, GenerateDraftRequest request)
        => await ExecuteCommandAsync(new GenerateDraftCommand(companyId, request), Ok);

    /// <summary>
    /// Rewrite item
    /// </summary>
    [HttpPut("rewrite/item")]
    [StandardApiResponses]
    public async Task<IActionResult> RewriteItem([FromBody] RewriteItemRequest request)
        => await ExecuteCommandAsync(new RewriteItemCommand(request), Ok);
}