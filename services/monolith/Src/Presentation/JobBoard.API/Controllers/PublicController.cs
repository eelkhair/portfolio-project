using JobBoard.Application.Actions.Public;
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
public class PublicController : BaseApiController
{
    /// <summary>
    /// Retrieves a list of publicly available jobs based on the specified filters.
    /// </summary>
    /// <param name="search">
    /// A keyword to filter job postings by title, description, or related metadata.
    /// </param>
    /// <param name="jobType">
    /// The type of job to filter the results by (e.g., FullTime, PartTime, Contract, Internship).
    /// </param>
    /// <param name="location">
    /// A location string to filter job postings by their geographical location.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing the list
    /// of jobs matching the specified criteria.
    /// </returns>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs([FromQuery] string? search, [FromQuery] JobType? jobType,
        [FromQuery] string? location, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await ExecuteQueryAsync(new ListPublicJobsQuery
        {
            Search = search, JobType = jobType, Location = location,
            Page = page, PageSize = pageSize
        }, Ok);
    }

    /// <summary>
    /// Retrieves the details of a specific job based on its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the job to retrieve.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing the
    /// details of the specified job.
    /// </returns>
    [HttpGet("jobs/{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        return await ExecuteQueryAsync(new GetPublicJobByIdQuery(id), Ok);
    }

    /// <summary>
    /// Retrieves a list of jobs similar to the specified job.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the job for which similar jobs are to be retrieved.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing a list
    /// of jobs that are similar to the specified job.
    /// </returns>
    [HttpGet("jobs/{id:guid}/similar")]
    public async Task<IActionResult> GetSimilarJobs(Guid id)
    {
        return await ExecuteQueryAsync(new GetSimilarJobsQuery(id), Ok);
    }

    /// <summary>
    /// Searches for job postings based on the specified criteria.
    /// </summary>
    /// <param name="query">
    /// A keyword or phrase to filter job postings by title, description, or other metadata.
    /// </param>
    /// <param name="jobType">
    /// The type of job to filter the results by (e.g., FullTime, PartTime, Contract, Internship).
    /// </param>
    /// <param name="location">
    /// A location string to filter job postings by their geographical location.
    /// </param>
    /// <param name="limit">
    /// The maximum number of job postings to return in the results. The default value is 30.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing the list
    /// of job postings matching the specified criteria.
    /// </returns>
    [HttpGet("jobs/search")]
    public async Task<IActionResult> SearchJobs([FromQuery] string? query, [FromQuery] string? jobType,
        [FromQuery] string? location, int limit = 30)
    {
        return await ExecuteQueryAsync(new SearchJobsQuery { Query = query, JobType = jobType, Location = location, Limit = limit }, Ok);
    }

    /// <summary>
    /// Retrieves the latest job postings, limited by the specified count.
    /// </summary>
    /// <param name="count">
    /// The maximum number of job postings to retrieve. Defaults to 6 if not specified.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing the list
    /// of the latest job postings.
    /// </returns>
    [HttpGet("jobs/latest")]
    public async Task<IActionResult> GetLatestJobs([FromQuery] int count = 6)
    {
        return await ExecuteQueryAsync(new GetLatestJobsQuery(count), Ok);
    }

    /// <summary>
    /// Retrieves a list of public companies available in the system.
    /// </summary>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing
    /// the list of public companies.
    /// </returns>
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        return await ExecuteQueryAsync(new ListPublicCompaniesQuery(), Ok);
    }

    /// <summary>
    /// Retrieves detailed information about a specific company.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the company whose details are being retrieved.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing
    /// the company's details.
    /// </returns>
    [HttpGet("companies/{id:guid}")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        return await ExecuteQueryAsync(new GetPublicCompanyByIdQuery(id), Ok);
    }

    /// <summary>
    /// Retrieves a list of jobs associated with a specific company.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the company for which the jobs are being retrieved.
    /// </param>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing
    /// a list of job postings associated with the specified company.
    /// </returns>
    [HttpGet("companies/{id:guid}/jobs")]
    public async Task<IActionResult> GetCompanyJobs(Guid id)
    {
        return await ExecuteQueryAsync(new GetCompanyJobsQuery(id), Ok);
    }

    /// <summary>
    /// Retrieves public statistics related to job postings and companies.
    /// </summary>
    /// <returns>
    /// An asynchronous operation that resolves to an IActionResult containing
    /// public statistics, such as the total number of jobs, companies, or any
    /// other publicly accessible metrics.
    /// </returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        return await ExecuteQueryAsync(new GetPublicStatsQuery(), Ok);
    }
}
