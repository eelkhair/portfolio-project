using JobBoard.API.Helpers;
using JobBoard.API.Infrastructure.Authorization;
using JobBoard.API.Infrastructure.SignalR.CompanyActivation;
using JobBoard.Application.Actions.Companies.Activate;
using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Application.Actions.Companies.Update;
using JobBoard.Application.Interfaces.Users;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// Company Controller 
/// </summary>
public class CompaniesController(IUserAccessor accessor, ICompanyActivationNotifier notifier) : BaseApiController
{
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
            CreatedOData("companies", result.Data!.Id, result));

    /// <summary>
    /// Update Company
    /// </summary>
    [HttpPut("{id:guid}")]
    [StandardApiResponses]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyCommand command)
    {
        command.Id = id;
        return await ExecuteCommandAsync(command, Ok);
    }

    /// <summary>
    /// Processes a request indicating that a company has been successfully created
    /// and activates the company using the provided data.
    /// </summary>
    /// <param name="request">The model containing details about the created company.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An IActionResult indicating the result of the activation operation.</returns>
    [HttpPost("company-created-success")]
    [Authorize(Policy = "DaprInternal")]
    public async Task<IActionResult> CompanyCreatedSuccess([FromBody] CompanyCreatedModel request, CancellationToken cancellationToken)
    {
        accessor.UserId = request.CreatedBy;

        await ExecuteCommandAsync(new ActivateCompanyCommand(request), Ok);
        
        await notifier.NotifyAsync(request, cancellationToken);
        return Ok();
    }
}