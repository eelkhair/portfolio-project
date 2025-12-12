using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;

namespace ConnectorAPI.Interfaces;

public interface IAdminApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload payload, CancellationToken cancellationToken);
}