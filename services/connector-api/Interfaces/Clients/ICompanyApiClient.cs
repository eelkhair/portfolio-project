using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;

namespace ConnectorAPI.Interfaces.Clients;

public interface ICompanyApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload companyApiPayload, CancellationToken cancellationToken);
    Task SendCompanyUpdatedAsync(Guid companyUId, CompanyUpdatedCompanyApiPayload payload, CancellationToken cancellationToken);
}