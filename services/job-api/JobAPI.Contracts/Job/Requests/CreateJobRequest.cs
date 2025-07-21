namespace JobAPI.Contracts.Job.Requests;

public record CreateJobRequest(
    string Title,
    string Company,
    string Location,
    string JobType,
    string SalaryRange,
    string PostedAt,
    string AboutCompany,
    string AboutRole,
    IEnumerator<string> Responsibilities,
    IEnumerable<string> Qualifications,
    string EEO
);