using ConnectorAPI.Models;

namespace ConnectorAPI.Interfaces;

public interface IAdminApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedPayload payload, CancellationToken cancellationToken);
}