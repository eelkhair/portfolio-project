using System.Text.Json.Serialization;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;
using JobBoard.AI.Infrastructure.Dapr.AITools.Shared;
using JobBoard.Monolith.Contracts.Companies;
using JobAPI.Contracts.Models.Jobs.Responses;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

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