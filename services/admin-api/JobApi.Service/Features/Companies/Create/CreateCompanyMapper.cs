

using JobAPI.Contracts.Models.Companies.Requests;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Infrastructure.Data.Entities;
using Mapster;

namespace JobApi.Features.Companies.Create;

public class Mappers : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity -> DTO
        config.NewConfig<Company, CompanyResponse>();

        // Request -> Entity
        config.NewConfig<CreateCompanyRequest, Company>()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
    }
}