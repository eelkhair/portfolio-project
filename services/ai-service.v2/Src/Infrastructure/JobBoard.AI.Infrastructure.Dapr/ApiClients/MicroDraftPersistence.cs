using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Interfaces.Clients;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public class MicroDraftPersistence(IAdminApiClient client) : IDraftPersistence
{
    public async Task<DraftResponse> SaveDraftAsync(Guid companyId, DraftResponse draft, CancellationToken ct)
    {
        var response = await client.SaveDraftAsync(companyId, new
        {
            draft.Title,
            draft.AboutRole,
            draft.Responsibilities,
            draft.Qualifications,
            draft.Notes,
            draft.Location,
            draft.JobType,
            draft.SalaryRange,
            Id = string.IsNullOrEmpty(draft.Id) ? null : draft.Id
        }, ct);

        draft.Id = response.Data?.Id ?? string.Empty;
        return draft;
    }
}
