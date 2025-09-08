using CompanyAPI.Contracts.Models.Industries.Responses;

namespace AdminApi.Application.Queries.Interfaces;

public interface IIndustryQueryService
{
    Task<List<IndustryResponse>> ListAsync(CancellationToken ct);
}