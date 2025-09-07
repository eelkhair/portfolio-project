using JobAPI.Contracts.Enums;
using JobAPI.Contracts.Models.Companies.Responses;

namespace JobAPI.Contracts.Models.Jobs.Responses;

public class JobResponse
{
    public Guid UId { get; set; }
    public string Title { get; set; }
    public CompanyResponse? Company { get; set; } = null!;
    public string Location { get; set; }
    public JobType JobType { get; set; }
    public string AboutRole { get; set; }
    public string? SalaryRange { get; set; }
    public List<string> Responsibilities { get; set; } = [];
    public List<string> Qualifications { get; set; } = [];
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
};
