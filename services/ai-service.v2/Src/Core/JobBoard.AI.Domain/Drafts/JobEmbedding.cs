
using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Common;
using Pgvector;

namespace JobBoard.AI.Domain.Drafts;

public sealed class JobEmbedding : Entity
{
    private JobEmbedding() { }
    public Guid JobId { get; private set; }
    

    public Vector VectorData { get; private set; } = default!;
    
    public EmbeddingVector Vector => new(VectorData.ToArray());

    public ProviderName Provider { get; private set; } = default!;
    public ModelName Model { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public JobEmbedding(
        Guid jobId,
        EmbeddingVector vector,
        ProviderName provider,
        ModelName model)
    {
        JobId = jobId;
        VectorData = new Vector(vector.Values);
        Provider = provider;
        Model = model;
        CreatedAt = DateTime.UtcNow;
    }
}
