using JobBoard.IntegrationEvents.Job;
using ReverseConnectorAPI.Models;

namespace ReverseConnectorAPI.Mappers;

public static class JobMapper
{
    public static SyncJobCreatePayload ToPayload(MicroJobCreatedV1Event evt)
        => new()
        {
            JobId = evt.UId,
            CompanyId = evt.CompanyUId,
            Title = evt.Title,
            AboutRole = evt.AboutRole,
            Location = evt.Location,
            SalaryRange = evt.SalaryRange,
            JobType = evt.JobType,
            Responsibilities = evt.Responsibilities,
            Qualifications = evt.Qualifications
        };
}
