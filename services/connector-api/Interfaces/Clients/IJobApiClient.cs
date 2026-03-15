using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.Drafts;
using ConnectorAPI.Models.JobCreated;

namespace ConnectorAPI.Interfaces.Clients;

public interface IJobApiClient
{
    Task SendCompanyCreatedAsync(EventDto<CompanyCreatedJobApiPayload> payload, CancellationToken cancellationToken);
    Task<JobApiResponse> SendJobCreatedAsync(EventDto<JobCreatedJobApiPayload> payload, CancellationToken cancellationToken);
    Task SendCompanyUpdatedAsync(Guid companyUId, EventDto<CompanyUpdatedJobApiPayload> payload, CancellationToken cancellationToken);

    // Draft CRUD — routed to job-api microservice
    Task<DraftResponse> SaveDraftAsync(Guid companyUId, EventDto<SaveDraftPayload> payload, CancellationToken cancellationToken);
    Task<List<DraftResponse>> ListDraftsAsync(Guid companyUId, CancellationToken cancellationToken);
    Task DeleteDraftAsync(Guid draftUId, string userId, CancellationToken cancellationToken);
    Task<DraftResponse?> GetDraftAsync(Guid draftUId, CancellationToken cancellationToken);
}