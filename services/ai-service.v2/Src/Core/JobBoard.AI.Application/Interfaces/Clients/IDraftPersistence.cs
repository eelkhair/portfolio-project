using JobBoard.AI.Application.Actions.Drafts;

namespace JobBoard.AI.Application.Interfaces.Clients;

public interface IDraftPersistence
{
    Task<DraftResponse> SaveDraftAsync(Guid companyId, DraftResponse draft, CancellationToken ct);
}
