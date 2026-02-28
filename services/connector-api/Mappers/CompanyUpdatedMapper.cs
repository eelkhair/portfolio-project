using ConnectorAPI.Models.CompanyUpdated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Mappers;

public record CompanyUpdatedPayloads(
    CompanyUpdatedCompanyApiPayload Company,
    CompanyUpdatedJobApiPayload Job);

public static class CompanyUpdatedMapper
{
    public static CompanyUpdatedPayloads Map(CompanyUpdatedV1Event evt, CompanyUpdateCompanyResult company)
    {
        return new CompanyUpdatedPayloads(
            new CompanyUpdatedCompanyApiPayload
            {
                Name = company.Name,
                CompanyEmail = company.Email,
                CompanyWebsite = company.Website,
                Phone = company.Phone,
                Description = company.Description,
                About = company.About,
                EEO = company.EEO,
                Founded = company.Founded,
                Size = company.Size,
                Logo = company.Logo,
                IndustryUId = company.IndustryId
            },
            new CompanyUpdatedJobApiPayload
            {
                Name = company.Name
            }
        );
    }
}
