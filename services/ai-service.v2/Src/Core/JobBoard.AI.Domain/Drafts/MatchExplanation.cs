using JobBoard.AI.Domain.Common;

namespace JobBoard.AI.Domain.Drafts;

public sealed class MatchExplanation : Entity
{
    private MatchExplanation() { }

    public Guid ResumeUId { get; private set; }
    public Guid JobId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string DetailsJson { get; private set; } = "[]";
    public string GapsJson { get; private set; } = "[]";
    public double Similarity { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public MatchExplanation(
        Guid resumeUId,
        Guid jobId,
        string summary,
        string detailsJson,
        string gapsJson,
        string provider,
        string model,
        double similarity = 0)
    {
        ResumeUId = resumeUId;
        JobId = jobId;
        Summary = summary;
        DetailsJson = detailsJson;
        GapsJson = gapsJson;
        Provider = provider;
        Model = model;
        Similarity = similarity;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string summary, string detailsJson, string gapsJson, string provider, string model, double similarity = 0)
    {
        Summary = summary;
        DetailsJson = detailsJson;
        GapsJson = gapsJson;
        Provider = provider;
        Model = model;
        Similarity = similarity;
        CreatedAt = DateTime.UtcNow;
    }
}
