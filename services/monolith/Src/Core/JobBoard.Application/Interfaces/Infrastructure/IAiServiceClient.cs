using JobBoard.Monolith.Contracts.Jobs;
using JobBoard.Monolith.Contracts.Settings;

namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IAiServiceClient
{
    Task<List<JobDraftResponse>> ListDrafts(Guid companyId, CancellationToken cancellationToken);
    Task<JobRewriteResponse> RewriteItem(JobRewriteRequest requestModel, CancellationToken cancellationToken);
    Task<JobGenResponse> GenerateDraft(Guid companyId, JobGenRequest requestModel, CancellationToken cancellationToken);
    Task<ProviderSettings> GetProvider(CancellationToken cancellationToken);
    Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken);
}