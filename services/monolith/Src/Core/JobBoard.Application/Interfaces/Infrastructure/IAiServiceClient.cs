using JobBoard.Application.Actions.Public;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Settings;

namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IAiServiceClient
{
    Task<List<DraftResponse>> ListDrafts(Guid companyId, CancellationToken cancellationToken);
    Task<DraftRewriteResponse> RewriteItem(DraftItemRewriteRequest requestModel, CancellationToken cancellationToken);
    Task<DraftGenResponse> GenerateDraft(Guid companyId, DraftGenRequest requestModel, CancellationToken cancellationToken);
    Task<DraftResponse> SaveDraft(Guid companyId, DraftResponse draft, CancellationToken cancellationToken);
    Task<ProviderSettings> GetProvider(CancellationToken cancellationToken);
    Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken);
    Task UpdateApplicationMode(ApplicationModeDto request, CancellationToken cancellationToken);
    Task<ApplicationModeDto> GetApplicationMode(CancellationToken cancellationToken);
    
    Task<List<JobCandidate>> GetSimilarJobs(Guid jobId, CancellationToken cancellationToken);
    Task<List<JobCandidate>> SearchJobs(string? query, string? location, string? jobType, int limit = 50, CancellationToken cancellationToken = default);
}