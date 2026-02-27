using ConnectorAPI.Models.JobCreated;
using JobBoard.IntegrationEvents.Job;

namespace ConnectorAPI.Mappers;

public static class JobCreatedMapper
{
    public static JobCreatedJobApiPayload Map(JobCreatedV1Event evt)
        => new()
        {
            Title = evt.Title,
            CompanyUId = evt.CompanyUId,
            Location = evt.Location,
            JobType = Enum.Parse<JobType>(evt.JobType, ignoreCase: true),
            AboutRole = evt.AboutRole,
            SalaryRange = evt.SalaryRange,
            Responsibilities = evt.Responsibilities,
            Qualifications = evt.Qualifications,
        };

}
