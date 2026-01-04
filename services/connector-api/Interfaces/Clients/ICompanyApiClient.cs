using ConnectorAPI.Models.CompanyCreated;

namespace ConnectorAPI.Interfaces.Clients;

public interface ICompanyApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload companyApiPayload, CancellationToken cancellationToken);
}