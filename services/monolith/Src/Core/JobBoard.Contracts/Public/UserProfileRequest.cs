using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Monolith.Contracts.Public;

public class UserProfileRequest
{
    public string? Phone { get; set; }
    public string? LinkedIn { get; set; }
    public string? Portfolio { get; set; }
    public string? Experience { get; set; }
    public List<string>? Skills { get; set; }
    public string? PreferredLocation { get; set; }
    public JobType? PreferredJobType { get; set; }
}
