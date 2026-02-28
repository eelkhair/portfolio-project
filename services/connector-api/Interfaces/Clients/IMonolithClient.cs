using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using JobBoard.IntegrationEvents.Company;

namespace ConnectorAPI.Interfaces.Clients;

public interface IMonolithClient
{
    Task<(CompanyCreateCompanyResult Company, CompanyCreateUserResult Admin)>
        GetCompanyAndAdminForCreatedEventAsync(
            Guid companyId,
            Guid adminId,
            string userId,
            CancellationToken cancellationToken);

    Task ActivateCompanyAsync(CompanyCreatedV1Event eventData, CompanyCreateCompanyResult company,
        CompanyCreatedUserApiPayload userApiResponse, CancellationToken cancellationToken);

    Task<CompanyUpdateCompanyResult> GetCompanyForUpdatedEventAsync(
        Guid companyId,
        string userId,
        CancellationToken cancellationToken);
}