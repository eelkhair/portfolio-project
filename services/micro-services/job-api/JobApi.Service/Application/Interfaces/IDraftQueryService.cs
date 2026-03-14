using JobAPI.Contracts.Models.Drafts.Responses;

namespace JobApi.Application.Interfaces;

public interface IDraftQueryService
{
    Task<List<DraftResponse>> ListDraftsAsync(Guid companyUId, CancellationToken ct);
    Task<DraftResponse?> GetDraftAsync(Guid draftUId, CancellationToken ct);
}
