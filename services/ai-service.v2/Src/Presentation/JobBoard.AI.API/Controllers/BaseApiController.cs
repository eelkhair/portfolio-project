using System.Net;
using JobBoard.AI.API.Helpers;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected async Task<IActionResult> ExecuteCommandAsync<TResult>(
        BaseCommand<TResult> command,
        Func<ApiResponse<TResult>, IActionResult> onSuccess)
    {
        try
        {
            var result = await ExecuteCoreAsync(command);
            return onSuccess(ApiResponse.Success(result));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    protected async Task<IActionResult> ExecuteQueryAsync<TResult>(
        BaseQuery<TResult> query,
        Func<ApiResponse<TResult>, IActionResult> onSuccess)
    {
        try
        {
            var result = await ExecuteCoreAsync(query);
            return onSuccess(ApiResponse.Success(result));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private Task<TResult> ExecuteCoreAsync<TResult>(IRequest<TResult> request)
    {
        var handlerType = typeof(IHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
        var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
        return ((dynamic)handler).HandleAsync((dynamic)request, HttpContext.RequestAborted);
    }

    private IActionResult HandleException(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException ex => Unauthorized(
                ApiResponse.Fail<object>(ex.Message, HttpStatusCode.Unauthorized)
            ),

            _ => StatusCode(
                500,
                ApiResponse.Fail<object>(
                    "An unexpected error occurred.",
                    HttpStatusCode.InternalServerError
                )
            )
        };
    }
}
