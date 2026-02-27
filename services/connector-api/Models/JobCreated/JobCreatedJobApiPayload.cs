namespace ConnectorAPI.Models.JobCreated;

public enum JobType
{
    FullTime,
    PartTime,
    Contract,
    Internship
}

public class JobCreatedJobApiPayload
{
    public required string Title { get; init; }
    public Guid CompanyUId { get; init; }
    public required string Location { get; init; }
    public required JobType JobType { get; init; }
    public required string AboutRole { get; init; }
    public string? SalaryRange { get; init; }
    public List<string> Responsibilities { get; init; } = [];
    public List<string> Qualifications { get; init; } = [];
}
