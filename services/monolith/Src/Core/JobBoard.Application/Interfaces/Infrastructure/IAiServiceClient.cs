using JobBoard.Application.Actions.Jobs.Models;
using JobBoard.Application.Actions.Settings.Models;

namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IAiServiceClient
{
    Task<List<JobDraftResponse>> ListDrafts(Guid companyId, CancellationToken cancellationToken);
    Task<JobRewriteResponse> RewriteItem(JobRewriteRequest requestModel, CancellationToken cancellationToken);
    Task<JobGenResponse> GenerateDraft(Guid companyId, JobGenRequest request, CancellationToken cancellationToken);
    Task<ProviderSettings> GetProvider(CancellationToken cancellationToken);
    Task UpdateProvider(UpdateProviderRequest request, CancellationToken cancellationToken);
}