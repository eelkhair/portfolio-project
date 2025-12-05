using JobBoard.Application.Actions.Companies.Get;
using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;

namespace JobBoard.API.Controllers.OData;

/// <summary>
/// The CompaniesController provides OData API endpoints for managing, querying, and interacting
/// with company-related resources. This controller inherits from the BaseODataController
/// to enable common OData functionality.
/// </summary>

public class CompaniesController : BaseODataController
{
    /// <summary>
    /// Retrieves a company by its unique identifier (UId).
    /// </summary>
    /// <param name="uId">The unique identifier of the company to retrieve.</param>
    /// <returns>A task that represents an asynchronous operation. The task result contains an <see cref="IQueryable{T}"/> of <see cref="CompanyDto"/> representing the company with the specified UId.</returns>
    [HttpGet("{uId:guid}")]
    [EnableQuery]
    public async Task<SingleResult> GetCompanyByUId(Guid uId)
    {
       var r = await ExecuteODataQueryAsync(new GetCompanyByUIdQuery{UId = uId});
       
       return SingleResult.Create(r);
    }
        
  }