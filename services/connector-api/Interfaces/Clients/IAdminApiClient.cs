using ConnectorAPI.Models.CompanyCreated;

namespace ConnectorAPI.Interfaces.Clients;

public interface IAdminApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload payload, CancellationToken cancellationToken);
}