
using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Common;
using Pgvector;

namespace JobBoard.AI.Domain.Drafts;

public sealed class DraftEmbedding : Entity
{
    private DraftEmbedding() { }
    public Guid DraftId { get; set; }
    public Draft Draft { get; private set; } = default!;
    public Vector VectorData { get; private set; } = default!;
    
    public EmbeddingVector Vector => new(VectorData.ToArray());

    public ProviderName Provider { get; private set; } = default!;
    public ModelName Model { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public DraftEmbedding(
        EmbeddingVector vector,
        ProviderName provider,
        ModelName model)
    {
        VectorData = new Vector(vector.Values);
        Provider = provider;
        Model = model;
        CreatedAt = DateTime.UtcNow;
    }
}
