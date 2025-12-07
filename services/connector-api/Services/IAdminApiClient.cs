using ConnectorAPI.Models;

namespace ConnectorAPI.Services;

public interface IAdminApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedPayload payload, CancellationToken cancellationToken);
}