using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyAPI.Contracts.Models.Companies.Responses;
using CompanyAPI.Contracts.Models.Industries.Responses;
using CompanyApi.Infrastructure.Data.Entities;
using Mapster;

namespace CompanyApi.Application;

public class Mappers : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        #region Company
        config.NewConfig<Company, CompanyResponse>();
        
        
        config.NewConfig<CreateCompanyRequest, Company>()
            .Map(dest => dest.Email, c=> c.CompanyEmail)
            .Map(dest => dest.Website, c=> c.CompanyWebsite)
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow);
        
        #endregion
        
        #region Industry
            config.NewConfig<Industry, IndustryResponse>();
        #endregion
    }
}