// In: JobBoard.API.Controllers.BaseApiController.cs

using System.Net;
using FluentValidation;
using JobBoard.AI.API.Helpers;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Infrastructure.Exceptions;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Base API Controller providing standardized execution methods for commands and queries.
/// </summary>
[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{

    /// <summary>
    /// Executes a standard command and returns the appropriate IActionResult.
    /// This should be the default choice for any write operation.
    /// </summary>
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

    /// <summary>
    /// Executes a read-only query and returns the appropriate IActionResult.
    /// </summary>
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

    /// <summary>
    /// Creates an OData-compliant response for a newly created resource, including a Location header with the resource's URI.
    /// </summary>
    /// <param name="entitySet">The name of the OData entity set containing the newly created resource.</param>
    /// <param name="id">The unique identifier of the newly created resource.</param>
    /// <param name="body">The ApiResponse containing the resource data and additional metadata.</param>
    /// <typeparam name="T">The type of the resource being created.</typeparam>
    /// <returns>A 201 Created IActionResult with the OData-specific Location header and response body.</returns>
    protected IActionResult CreatedOData<T>(
        string entitySet,
        Guid id,
        ApiResponse<T> body)
    {
        Response.Headers.Location =
            $"{Request.Scheme}://{Request.Host}/odata/{entitySet}({id})";

        return StatusCode(StatusCodes.Status201Created, body);
    }
    protected Task<TResult> ExecuteCoreAsync<TResult>(IRequest<TResult> request)
    {
        var handlerType = typeof(IHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
        var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
        return ((dynamic)handler).HandleAsync((dynamic)request, HttpContext.RequestAborted);
    }
    
    private IActionResult HandleException(Exception exception)
    {
        return exception switch
        {
            ValidationException vex => BadRequest(
                ApiResponse.Fail<object>(
                    "Validation failed",
                    HttpStatusCode.BadRequest,
                    vex.Errors.ToDictionary(
                        e => e.PropertyName,
                        e => new[] { e.ErrorMessage }
                    )
                )
            ),

            UnauthorizedAccessException ex => Unauthorized(
                ApiResponse.Fail<object>(ex.Message, HttpStatusCode.Unauthorized)
            ),

            ForbiddenAccessException ex => StatusCode(
                403, ApiResponse.Fail<object>(ex.Message, HttpStatusCode.Forbidden)
            ),

            NotFoundException ex => NotFound(
                ApiResponse.Fail<object>(ex.Message, HttpStatusCode.NotFound)
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