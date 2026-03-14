using System.Security.Claims;
using JobAPI.Contracts.Models.Drafts.Requests;
using JobAPI.Contracts.Models.Drafts.Responses;

namespace JobApi.Application.Interfaces;

public interface IDraftCommandService
{
    Task<DraftResponse> SaveDraftAsync(Guid companyUId, SaveDraftRequest request, ClaimsPrincipal user, CancellationToken ct);
    Task DeleteDraftAsync(Guid draftUId, ClaimsPrincipal user, CancellationToken ct);
}
