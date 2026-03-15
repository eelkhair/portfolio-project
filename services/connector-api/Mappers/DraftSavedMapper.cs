using ConnectorAPI.Models.Drafts;
using JobBoard.IntegrationEvents.Draft;

namespace ConnectorAPI.Mappers;

public static class DraftSavedMapper
{
    public static SaveDraftPayload Map(DraftSavedV1Event evt)
        => new()
        {
            Id = evt.UId.ToString(),
            Title = evt.Title,
            AboutRole = evt.AboutRole,
            Location = evt.Location,
            JobType = evt.JobType,
            SalaryRange = evt.SalaryRange ?? "",
            Notes = evt.Notes,
            Responsibilities = evt.Responsibilities,
            Qualifications = evt.Qualifications,
        };
}
