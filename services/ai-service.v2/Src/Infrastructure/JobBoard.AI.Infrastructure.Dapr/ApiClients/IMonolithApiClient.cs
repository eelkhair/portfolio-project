using System.Text.Json.Serialization;
using JobBoard.Monolith.Contracts.Companies;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public interface IMonolithApiClient
{
    Task<ODataResponse<List<CompanyDto>>> ListCompaniesAsync(CancellationToken cancellationToken = default);
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyCommand cmd, CancellationToken ct);
    Task<ODataResponse<List<IndustryDto>>> ListIndustriesAsync(CancellationToken ct);
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