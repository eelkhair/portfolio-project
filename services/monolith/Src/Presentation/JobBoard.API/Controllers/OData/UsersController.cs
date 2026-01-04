using JobBoard.Application.Actions.Users.Get;
using JobBoard.Application.Actions.Users.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;

namespace JobBoard.API.Controllers.OData;

/// <summary>
/// Controller class for managing user-related operations.
/// Acts as an OData endpoint for querying and retrieving user data.
/// </summary>
public class UsersController : BaseODataController
{
    /// <summary>
    /// Retrieves a collection of user data with support for OData querying.
    /// </summary>
    /// <returns>
    /// An asynchronous task that returns an <c>IQueryable</c> of <c>UserDto</c>,
    /// representing the user data that matches the filter, sort, or other OData query options.
    /// </returns>
    [HttpGet]
    [EnableQuery]
    public async Task<IQueryable<UserDto>> Get()
    {
        return await ExecuteODataQueryAsync(new GetUsersQuery());
    }

    /// <summary>
    /// Retrieves a specific user's data identified by their unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the user to retrieve.
    /// </param>
    /// <returns>
    /// A <c>SingleResult</c> containing the user data matching the specified identifier.
    /// </returns>
    [HttpGet("odata/users({id})")]
    [EnableQuery]
    public async Task<SingleResult> GetUserById(Guid id)
    {
        var users =  await ExecuteODataQueryAsync(new GetUsersQuery());

        return SingleResult.Create(users.Where(c => c.Id == id));
    }
}