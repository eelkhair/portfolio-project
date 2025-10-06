namespace AdminAPI.Contracts.Models.Jobs.Responses;

public class JobGenResponse
{
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();

    /// <summary>Required; empty string if no caveats.</summary>
    public string Notes { get; set; } = "";

    /// <summary>Required; "" if unknown. Normalized to "City, ST" | "Remote" | "Hybrid".</summary>
    public string Location { get; set; } = "";

    public JobGenMetadata Metadata { get; set; } = new();
}