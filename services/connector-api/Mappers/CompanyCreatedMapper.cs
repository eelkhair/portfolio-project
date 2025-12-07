using ConnectorAPI.Models;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Mappers;

public static class CompanyCreatedMapper
{
    public static CompanyCreatedPayload Map(
        CompanyCreatedV1Event evt,
        CompanyCreateCompanyResult company,
        CompanyCreateUserResult admin)
    {
        return new CompanyCreatedPayload
        {
            CompanyId      = evt.CompanyUId,
            CompanyName    = company.Name,
            CompanyEmail   = company.Email,
            Website        = company.Website,
            IndustryId     = company.IndustryId,
            AdminUserId    = admin.Id,
            AdminFirstName = admin.FirstName,
            AdminLastName  = admin.LastName,
            AdminEmail     = admin.Email
        };
    }
}