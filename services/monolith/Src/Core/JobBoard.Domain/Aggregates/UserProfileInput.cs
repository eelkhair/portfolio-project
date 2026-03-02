using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Domain.Aggregates;

public class UserProfileInput
{
    public int UserId { get; set; }
    public string? Phone { get; set; }
    public string? LinkedIn { get; set; }
    public string? Portfolio { get; set; }
    public string? Experience { get; set; }
    public List<string>? Skills { get; set; }
    public string? PreferredLocation { get; set; }
    public JobType? PreferredJobType { get; set; }

    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int InternalId { get; set; }
    public Guid UId { get; set; }
}
