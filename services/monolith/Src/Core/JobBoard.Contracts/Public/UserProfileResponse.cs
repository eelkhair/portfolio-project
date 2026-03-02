using JobBoard.Monolith.Contracts.Jobs;

namespace JobBoard.Monolith.Contracts.Public;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? LinkedIn { get; set; }
    public string? Portfolio { get; set; }
    public string? Experience { get; set; }
    public List<string> Skills { get; set; } = [];
    public string? PreferredLocation { get; set; }
    public JobType? PreferredJobType { get; set; }
}
