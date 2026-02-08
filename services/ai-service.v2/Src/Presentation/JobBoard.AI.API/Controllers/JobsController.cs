using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.GenerateJob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;



/// <summary>
/// Jobs Controller 
/// </summary>
public class JobsController : BaseApiController
{
    /// <summary>
    /// Generate Job
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    [StandardApiResponses]
   
    public async Task<IActionResult> Generate(GenerateJobRequest command)
        => await ExecuteCommandAsync(new GenerateJobCommand(command), Ok);
    
}