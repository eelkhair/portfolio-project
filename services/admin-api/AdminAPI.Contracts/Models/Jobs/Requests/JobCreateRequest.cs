using JobAPI.Contracts.Models.Jobs.Requests;

namespace AdminAPI.Contracts.Models.Jobs.Requests;

public class JobCreateRequest: CreateJobRequest
{
    public string DraftId {get;set;} = null!;
    public bool DeleteDraft {get;set;} = false;
}