using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.DemoCompany;

namespace ConnectorAPI.Interfaces.Clients;

public interface IUserApiClient
{
    Task<CompanyCreatedUserApiPayload> SendCompanyCreatedAsync(EventDto<CompanyCreatedUserApiPayload> payload, CancellationToken cancellationToken);

    /// <summary>Tear down the Keycloak group + provisioned users for a demo company.</summary>
    Task DeleteCompanyAsync(Guid companyUId, CancellationToken cancellationToken);

    /// <summary>Swap the synthetic demo admin user for the new claimer (Keycloak group membership + user-api projection).</summary>
    Task ClaimCompanyAsync(Guid companyUId, DemoCompanyClaimedPayload payload, CancellationToken cancellationToken);
}
