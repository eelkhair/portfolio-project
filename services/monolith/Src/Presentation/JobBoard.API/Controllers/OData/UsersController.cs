// using JobBoard.Application.Actions.Companies.Get;
// using JobBoard.Application.Actions.Users.Get;
// using JobBoard.Application.Actions.Users.Models;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.OData.Query;
// using Microsoft.AspNetCore.OData.Results;
//
// namespace JobBoard.API.Controllers.OData;
//
// /// <summary>
// /// Controller responsible for handling OData requests related to users.
// /// Inherits from BaseODataController to provide shared functionality for OData-based operations.
// /// </summary>
// public class UsersController : BaseODataController
// {
//     [HttpGet("Users")]
//     [EnableQuery]
//     public async Task<IQueryable<UserDto>> Get()
//     {
//         return await ExecuteODataQueryAsync(new GetUsersQuery());
//     }
//
//     [HttpGet("odata/Companies({uId})")]
//     [EnableQuery]
//     public SingleResult GetUserById(Guid uId)
//     {
//         var users =  ExecuteODataQueryAsync(new GetUsersQuery()).Result;
//
//         return SingleResult.Create(users.Where(c => c.UId == uId));
//     }
// }