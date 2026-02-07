using JobBoard.AI.API.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;



/// <summary>
/// Company Controller 
/// </summary>
public class CompaniesController : BaseApiController
{
    /// <summary>
    /// Create Company
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    [StandardApiResponses]
   
    public async Task<IActionResult> Create()
        => Ok("Company created successfully");
    
}