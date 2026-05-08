using ConnectorAPI.Models;
using ConnectorAPI.Models.CompanyCreated;
using ConnectorAPI.Models.CompanyUpdated;
using ConnectorAPI.Models.DemoCompany;
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

    /// <summary>Hard-delete a demo company's jobs from job-api.</summary>
    Task DeleteCompanyAsync(Guid companyUId, CancellationToken cancellationToken);

    /// <summary>Clear IsDemo flag on job-api projection + repoint admin reference.</summary>
    Task ClaimCompanyAsync(Guid companyUId, DemoCompanyClaimedPayload payload, CancellationToken cancellationToken);
}
