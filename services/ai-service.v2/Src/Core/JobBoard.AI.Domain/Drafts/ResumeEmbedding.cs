using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Common;
using Pgvector;

namespace JobBoard.AI.Domain.Drafts;

public sealed class ResumeEmbedding : Entity
{
    private ResumeEmbedding() { }

    public Guid ResumeUId { get; private set; }

    public Vector VectorData { get; private set; } = default!;

    public EmbeddingVector Vector => new(VectorData.ToArray());

    public ProviderName Provider { get; private set; } = default!;
    public ModelName Model { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public ResumeEmbedding(
        Guid resumeUId,
        EmbeddingVector vector,
        ProviderName provider,
        ModelName model)
    {
        ResumeUId = resumeUId;
        VectorData = new Vector(vector.Values);
        Provider = provider;
        Model = model;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(EmbeddingVector vector, ProviderName provider, ModelName model)
    {
        VectorData = new Vector(vector.Values);
        Provider = provider;
        Model = model;
        CreatedAt = DateTime.UtcNow;
    }
}
