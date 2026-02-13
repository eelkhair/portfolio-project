using JobBoard.Application.Actions.Drafts;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Drafts Controller
/// </summary>
public class DraftsController : BaseApiController
{
    /// <summary>
    /// Generate a job draft via AI
    /// </summary>
    [HttpPost("{companyId:guid}/generate")]
    public async Task<IActionResult> Generate(Guid companyId, JobGenRequest request)
        => await ExecuteCommandAsync(new GenerateDraftCommand
        {
            CompanyId = companyId,
            Request = request
        }, Ok);
    
    
}
