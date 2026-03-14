using JobBoard.AI.Application.Actions.Drafts;
using JobBoard.AI.Application.Interfaces.Clients;
using MonolithDraftResponse = JobBoard.Monolith.Contracts.Drafts.DraftResponse;

namespace JobBoard.AI.Infrastructure.Dapr.ApiClients;

public class MonolithDraftPersistence(IMonolithApiClient client) : IDraftPersistence
{
    public async Task<DraftResponse> SaveDraftAsync(Guid companyId, DraftResponse draft, CancellationToken ct)
    {
        var monolithDraft = new MonolithDraftResponse
        {
            Title = draft.Title,
            AboutRole = draft.AboutRole,
            Responsibilities = draft.Responsibilities,
            Qualifications = draft.Qualifications,
            Notes = draft.Notes,
            Location = draft.Location,
            JobType = draft.JobType,
            SalaryRange = draft.SalaryRange,
            Id = string.IsNullOrEmpty(draft.Id) ? null : draft.Id
        };

        var saved = await client.SaveDraftAsync(companyId, monolithDraft, ct);

        draft.Id = saved.Id ?? string.Empty;
        return draft;
    }
}
