using System.Text.Json.Serialization;
using JobBoard.AI.Infrastructure.Dapr.AITools.Monolith.Companies;
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