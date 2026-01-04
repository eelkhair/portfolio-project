using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;

namespace ConnectorAPI.Interfaces.Clients;

public interface IUserApiClient
{
    Task<CompanyCreatedUserApiPayload> SendCompanyCreatedAsync(EventDto<CompanyCreatedUserApiPayload> payload, CancellationToken cancellationToken);
}