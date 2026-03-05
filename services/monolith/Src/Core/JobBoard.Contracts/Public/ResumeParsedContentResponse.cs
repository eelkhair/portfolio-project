namespace JobBoard.Monolith.Contracts.Public;

// ProjectDto stays here for monolith-internal API contracts
// (SubmitApplicationRequest, UserProfileRequest, ApplicationResponse, etc.)
// ResumeParsedContentResponse moved to JobBoard.IntegrationEvents.Resume package.

public class ProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Technologies { get; set; } = [];
    public string? Url { get; set; }
}
