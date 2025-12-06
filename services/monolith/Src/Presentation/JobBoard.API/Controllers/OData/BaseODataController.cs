using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace JobBoard.API.Controllers.OData;

/// <summary>
/// Base OData controller.
/// </summary>
[Authorize]
public class BaseODataController : ODataController
{
    /// <summary>
    /// Executes an OData query.
    /// </summary>
    /// <param name="query"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    protected async Task<TResult> ExecuteODataQueryAsync<TResult>(
        BaseQuery<TResult> query)
    {

        var handlerType = typeof(IHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        dynamic handler = HttpContext.RequestServices.GetRequiredService(handlerType);

        return await handler.HandleAsync((dynamic)query, HttpContext.RequestAborted);

    }
}