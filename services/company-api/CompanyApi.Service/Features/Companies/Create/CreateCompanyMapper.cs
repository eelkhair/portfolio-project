using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyApi.Infrastructure.Data.Entities;
using Mapster;

namespace CompanyApi.Features.Companies.Create;

public class Mappers : IRegister
{
    public void Register(TypeAdapterConfig config)
    {

        config.NewConfig<Company, CompanyResponse>();
        
        config.NewConfig<CreateCompanyRequest, Company>()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
    }
}