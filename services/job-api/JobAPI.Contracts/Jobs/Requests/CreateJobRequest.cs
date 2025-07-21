namespace JobAPI.Contracts.Jobs.Requests;

public record CreateJobRequest(
    string Title,
    string Company,
    string Location,
    string JobType,
    string SalaryRange,
    string PostedAt,
    string AboutCompany,
    string AboutRole,
    List<string> Responsibilities,
    List<string> Qualifications,
    string EEO
);