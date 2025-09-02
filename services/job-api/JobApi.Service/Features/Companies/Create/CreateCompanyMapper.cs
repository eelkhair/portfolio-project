using FastEndpoints;
using JobAPI.Contracts.Models.Companies.Requests;
using JobAPI.Contracts.Models.Companies.Responses;
using JobApi.Infrastructure.Data.Entities;

namespace JobApi.Features.Companies.Create;

public class CompanyMapper: Mapper<CreateCompanyRequest, CompanyResponse, Company>
{
    public override Company ToEntity(CreateCompanyRequest src)=> new()
    {
        Name = src.Name,
        About = src.About,
        EEO = src.EEO
    };

    public override CompanyResponse FromEntity(Company e) => new()
    {
        Name = e.Name,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        UId = e.UId,
        About = e.About,
        EEO = e.EEO
    };
}