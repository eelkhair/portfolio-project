using System.Text.Json.Serialization;
using Elkhair.Dev.Common.Application;
using JobBoard.AI.Application.Interfaces.Resumes;
using JobBoard.Monolith.Contracts.Companies;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobBoard.AI.Application.Interfaces.Clients;

public interface IMonolithApiClient
{
    Task<ODataResponse<List<CompanyDto>>> ListCompaniesAsync(CancellationToken cancellationToken = default);
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyCommand cmd, CancellationToken ct);
    Task<CompanyDto> UpdateCompanyAsync(Guid companyId, UpdateCompanyCommand cmd, CancellationToken ct);
    Task<ODataResponse<List<IndustryDto>>> ListIndustriesAsync(CancellationToken ct);
    Task<ApiResponse<object>> CreateJobAsync(object cmd, CancellationToken ct);
    Task<List<JobResponse>> ListJobsAsync(Guid companyUId, CancellationToken ct);
    Task<List<CompanyJobSummaryDto>> ListCompanyJobSummariesAsync(CancellationToken ct);
    Task NotifyResumeParseCompletedAsync(ResumeParseCompletedRequest model, CancellationToken ct);
    Task NotifyResumeParseFailedAsync(ResumeParseFailedRequest model, CancellationToken ct);
    Task<ResumeParsedContentResponse?> GetResumeParsedContentAsync(Guid resumeUId, CancellationToken ct);
}

public class ResumeParseCompletedRequest
{
    public Guid ResumeUId { get; set; }
    public object ParsedContent { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}

public class ResumeParseFailedRequest
{
    public Guid ResumeUId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CurrentPage { get; set; }
}

public sealed class ODataResponse<T>
{
    [JsonPropertyName("value")]
    public T Value { get; init; } = default!;
}

public class CreateCompanyCommand
{
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public Guid IndustryUId { get; set; }
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
}

public class UpdateCompanyCommand
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public Guid IndustryUId { get; set; }
}
