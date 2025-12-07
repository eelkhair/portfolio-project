using JobBoard.Application.Actions.Users.Get;
using JobBoard.Application.Actions.Users.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;

namespace JobBoard.API.Controllers.OData;

public class UsersController : BaseODataController
{
    [HttpGet]
    [EnableQuery]
    public async Task<IQueryable<UserDto>> Get()
    {
        return await ExecuteODataQueryAsync(new GetUsersQuery());
    }

    [HttpGet("odata/users({id})")]
    [EnableQuery]
    public SingleResult GetUserById(Guid id)
    {
        var users =  ExecuteODataQueryAsync(new GetUsersQuery()).Result;

        return SingleResult.Create(users.Where(c => c.Id == id));
    }
}