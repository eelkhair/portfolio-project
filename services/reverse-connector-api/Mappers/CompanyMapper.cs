using JobBoard.IntegrationEvents.Company;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Mappers;

public static class CompanyMapper
{
    public static SyncCompanyCreatePayload ToCreatePayload(MicroCompanyCreatedV1Event evt)
        => new()
        {
            CompanyId = evt.CompanyUId,
            Name = evt.Name,
            CompanyEmail = evt.CompanyEmail,
            CompanyWebsite = evt.CompanyWebsite,
            IndustryUId = evt.IndustryUId,
            AdminFirstName = evt.AdminFirstName,
            AdminLastName = evt.AdminLastName,
            AdminEmail = evt.AdminEmail,
            AdminUId = evt.AdminUId,
            UserCompanyUId = evt.UserCompanyUId
        };

    public static SyncCompanyUpdatePayload ToUpdatePayload(MicroCompanyUpdatedV1Event evt)
        => new()
        {
            CompanyId = evt.CompanyUId,
            Name = evt.Name,
            CompanyEmail = evt.CompanyEmail,
            CompanyWebsite = evt.CompanyWebsite,
            Phone = evt.Phone,
            Description = evt.Description,
            About = evt.About,
            EEO = evt.EEO,
            Founded = evt.Founded,
            Size = evt.Size,
            Logo = evt.Logo,
            IndustryUId = evt.IndustryUId
        };
}
