using JobBoard.AI.Domain.Common;

namespace JobBoard.AI.Domain.Drafts;

public sealed class Draft : AggregateRoot
{
    private readonly List<JobEmbedding> _embeddings = [];

    private Draft() { } // EF
    public Guid CompanyId { get; private set; }
    public DraftType Type { get; private set; } = DraftType.Job;
    public DraftStatus Status { get; private set; } = DraftStatus.Draft;
    public string ContentJson { get; private set; } = "{}";

    public IReadOnlyCollection<JobEmbedding> Embeddings => _embeddings;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Draft(Guid companyId, DraftType type)
    {
        CompanyId = companyId;
        Type = type;
        CreatedAt = UpdatedAt = DateTime.UtcNow;
    }

    public void SetContent(string json)
    {
        ContentJson = json;
        Status = DraftStatus.Generated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddEmbedding(JobEmbedding embedding)
    {
        _embeddings.Add(embedding);
        UpdatedAt = DateTime.UtcNow;
    }
}