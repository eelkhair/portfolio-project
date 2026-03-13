using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.JobCreated;

namespace ConnectorAPI.Interfaces.Clients;

public interface IJobApiClient
{
    Task SendCompanyCreatedAsync(EventDto<CompanyCreatedJobApiPayload> payload, CancellationToken cancellationToken);
    Task<JobApiResponse> SendJobCreatedAsync(EventDto<JobCreatedJobApiPayload> payload, CancellationToken cancellationToken);
    Task SendCompanyUpdatedAsync(Guid companyUId, EventDto<CompanyUpdatedJobApiPayload> payload, CancellationToken cancellationToken);
}