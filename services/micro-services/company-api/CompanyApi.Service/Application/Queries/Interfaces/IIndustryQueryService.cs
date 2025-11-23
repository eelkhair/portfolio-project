using CompanyAPI.Contracts.Models.Industries.Responses;

namespace CompanyApi.Application.Queries.Interfaces;

public interface IIndustryQueryService
{
    Task<List<IndustryResponse>> ListAsync(CancellationToken ct);
}