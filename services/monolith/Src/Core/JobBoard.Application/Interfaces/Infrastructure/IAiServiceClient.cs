using JobBoard.Application.Actions.Public;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Contracts.Settings;

namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IAiServiceClient
{
    Task<DraftRewriteResponse> RewriteItem(DraftItemRewriteRequest requestModel, CancellationToken cancellationToken);
    Task<DraftGenResponse> GenerateDraft(Guid companyId, DraftGenRequest requestModel, CancellationToken cancellationToken);
    Task<ProviderSettings> GetProvider(CancellationToken cancellationToken);
    Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken);
    Task UpdateApplicationMode(ApplicationModeDto request, CancellationToken cancellationToken);
    Task<ApplicationModeDto> GetApplicationMode(CancellationToken cancellationToken);
    Task<List<JobCandidate>> GetSimilarJobs(Guid jobId, CancellationToken cancellationToken);
    Task<List<JobCandidate>> SearchJobs(string? query, string? location, string? jobType, int limit = 50, CancellationToken cancellationToken = default);
    Task<List<JobCandidate>> GetMatchingJobsForResumeAsync(Guid resumeId, int requestLimit, CancellationToken cancellationToken);
    Task<ReEmbedAllJobsResponse> ReEmbedAllJobs(CancellationToken cancellationToken);
}
