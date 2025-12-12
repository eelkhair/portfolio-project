using ConnectorAPI.Models.CompanyCreated;

namespace ConnectorAPI.Interfaces;

public interface ICompanyApiClient
{
    Task SendCompanyCreatedAsync(CompanyCreatedCompanyApiPayload companyApiPayload, CancellationToken cancellationToken);
}