using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Mappers;

public record CompanyCreatedPayloads(CompanyCreatedCompanyApiPayload Company, 
    EventDto<CompanyCreatedJobApiPayload> Job,
    EventDto<CompanyCreatedUserApiPayload> User);

public static class CompanyCreatedMapper
{
    public static CompanyCreatedPayloads Map(CompanyCreatedV1Event evt,
        CompanyCreateCompanyResult company, CompanyCreateUserResult admin)
    {
        return new CompanyCreatedPayloads
        (
            new CompanyCreatedCompanyApiPayload
            {
                CompanyId = evt.CompanyUId,
                Name = company.Name,
                CompanyEmail = company.Email,
                CompanyWebsite = company.Website,
                IndustryUId = company.IndustryUId,
                UserId = evt.UserId
            },
            new EventDto<CompanyCreatedJobApiPayload>(evt.UserId, Guid.CreateVersion7().ToString(),
                new CompanyCreatedJobApiPayload
                {
                    UId = evt.CompanyUId,
                    Name = company.Name
                }),
            new EventDto<CompanyCreatedUserApiPayload>(evt.UserId, Guid.CreateVersion7().ToString(),
                new CompanyCreatedUserApiPayload
                {
                    UId = evt.AdminUId,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName,
                    Email = admin.Email,
                    CompanyName = company.Name,
                    CompanyUId = evt.CompanyUId,
                    UserCompanyUId = evt.UserCompanyUId
                })
        );

    }
    
}


