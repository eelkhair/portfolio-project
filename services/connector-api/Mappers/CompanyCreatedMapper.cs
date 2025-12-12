using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Mappers;

public static class CompanyCreatedMapper
{
    public static CompanyCreatedCompanyApiPayload Map(
        CompanyCreatedV1Event evt,
        CompanyCreateCompanyResult company)
    {
        return new CompanyCreatedCompanyApiPayload
        {
            CompanyId = evt.CompanyUId,
            Name = company.Name,
            CompanyEmail = company.Email,
            CompanyWebsite = company.Website,
            IndustryUId = company.IndustryId,
            UserId = evt.UserId
        };
    }
}