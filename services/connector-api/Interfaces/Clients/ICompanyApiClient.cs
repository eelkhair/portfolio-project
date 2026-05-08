using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.DemoCompany;

namespace ConnectorAPI.Interfaces.Clients;

public interface ICompanyApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload companyApiPayload, CancellationToken cancellationToken);
    Task SendCompanyUpdatedAsync(Guid companyUId, CompanyUpdatedCompanyApiPayload payload, CancellationToken cancellationToken);

    /// <summary>Hard-delete a demo company (and its dependents) from company-api.</summary>
    Task DeleteCompanyAsync(Guid companyUId, CancellationToken cancellationToken);

    /// <summary>Repoint admin user reference + clear IsDemo flag on company-api projection.</summary>
    Task ClaimCompanyAsync(Guid companyUId, DemoCompanyClaimedPayload payload, CancellationToken cancellationToken);
}
