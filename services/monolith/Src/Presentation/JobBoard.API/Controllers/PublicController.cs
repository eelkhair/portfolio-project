using System.Net;
using FluentValidation;
using JobBoard.API.Helpers;
using JobBoard.Application.Actions.Public;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Controllers;

/// <summary>
/// The PublicController is responsible for handling public-facing API endpoints
/// related to job postings, companies, and statistics. This controller provides
/// methods for retrieving job listings, specific job details, similar jobs,
/// company information, and general public statistics.
/// </summary>
/// <remarks>
/// All endpoints in this controller are accessible without authentication.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
[Produces("application/json")]
public class PublicController : ControllerBase
{
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] string? search, [FromQuery] JobType? jobType, [FromQuery] string? location)
    {
        return await ExecuteQueryAsync(new ListPublicJobsQuery { Search = search, JobType = jobType, Location = location },
            Ok);
    }

    [HttpGet("jobs/{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        return await ExecuteQueryAsync(new GetPublicJobByIdQuery(id), Ok);
    }

    [HttpGet("jobs/{id:guid}/similar")]
    public async Task<IActionResult> GetSimilarJobs(Guid id, [FromQuery] Guid companyUId, [FromQuery] JobType jobType)
    {
        return await ExecuteQueryAsync(new GetSimilarJobsQuery(id, companyUId, jobType), Ok);
    }

    [HttpGet("jobs/latest")]
    public async Task<IActionResult> GetLatestJobs([FromQuery] int count = 6)
    {
        return await ExecuteQueryAsync(new GetLatestJobsQuery(count), Ok);
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        return await ExecuteQueryAsync(new ListPublicCompaniesQuery(), Ok);
    }

    [HttpGet("companies/{id:guid}")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        return await ExecuteQueryAsync(new GetPublicCompanyByIdQuery(id), Ok);
    }

    [HttpGet("companies/{id:guid}/jobs")]
    public async Task<IActionResult> GetCompanyJobs(Guid id)
    {
        return await ExecuteQueryAsync(new GetCompanyJobsQuery(id), Ok);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        return await ExecuteQueryAsync(new GetPublicStatsQuery(), Ok);
    }

    private async Task<IActionResult> ExecuteQueryAsync<TResult>(
        IRequest<TResult> request,
        Func<ApiResponse<TResult>, IActionResult> onSuccess)
    {
        try
        {
            var handlerType = typeof(IHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
            var handler = HttpContext.RequestServices.GetRequiredService(handlerType);
            var result = await ((dynamic)handler).HandleAsync((dynamic)request, HttpContext.RequestAborted);
            return onSuccess(ApiResponse.Success((TResult)result));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private IActionResult HandleException(Exception exception)
    {
        return exception switch
        {
            ValidationException vex => BadRequest(
                ApiResponse.Fail<object>(
                    "Validation failed",
                    HttpStatusCode.BadRequest,
                    vex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
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
