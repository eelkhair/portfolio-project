using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.JobCreated;

namespace ConnectorAPI.Interfaces.Clients;

public interface IJobApiClient
{
    Task SendCompanyCreatedAsync(EventDto<CompanyCreatedJobApiPayload> payload, CancellationToken cancellationToken);
    Task<JobApiResponse> SendJobCreatedAsync(JobCreatedJobApiPayload payload, CancellationToken cancellationToken);
}