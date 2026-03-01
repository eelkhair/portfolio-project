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
    
    Task<List<SimilarJobCandidate>> GetSimilarJobs(Guid jobId, CancellationToken cancellationToken);
}