using JobBoard.API.Helpers;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Application.Actions.Companies.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Company Controller 
/// </summary>
public class CompanyController : BaseApiController
{

    /// <summary>
    /// Get Company by UId
    /// </summary>
    /// <param name="uId"></param>
    /// <returns></returns>
    [HttpGet("{uId:guid}")]
    [StandardApiResponses]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid uId)
    {
        return Ok();
    }
    
    /// <summary>
    /// Create Company
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [HttpPost]
    [StandardApiResponses]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyCommand command)
        => await ExecuteCommandAsync(command, result =>
            CreatedAtAction(
                nameof(Get), 
                new { uId = result.Data!.Id },
                result
            ));
}