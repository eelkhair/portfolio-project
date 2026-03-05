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

    public Vector? SkillsVectorData { get; private set; }
    public EmbeddingVector? SkillsVector => SkillsVectorData is not null ? new(SkillsVectorData.ToArray()) : null;

    public Vector? ExperienceVectorData { get; private set; }
    public EmbeddingVector? ExperienceVector => ExperienceVectorData is not null ? new(ExperienceVectorData.ToArray()) : null;

    public ProviderName Provider { get; private set; } = default!;
    public ModelName Model { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public ResumeEmbedding(
        Guid resumeUId,
        EmbeddingVector vector,
        ProviderName provider,
        ModelName model,
        EmbeddingVector? skillsVector = null,
        EmbeddingVector? experienceVector = null)
    {
        ResumeUId = resumeUId;
        VectorData = new Vector(vector.Values);
        SkillsVectorData = skillsVector is not null ? new Vector(skillsVector.Values) : null;
        ExperienceVectorData = experienceVector is not null ? new Vector(experienceVector.Values) : null;
        Provider = provider;
        Model = model;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        EmbeddingVector vector,
        ProviderName provider,
        ModelName model,
        EmbeddingVector? skillsVector = null,
        EmbeddingVector? experienceVector = null)
    {
        VectorData = new Vector(vector.Values);
        SkillsVectorData = skillsVector is not null ? new Vector(skillsVector.Values) : null;
        ExperienceVectorData = experienceVector is not null ? new Vector(experienceVector.Values) : null;
        Provider = provider;
        Model = model;
        CreatedAt = DateTime.UtcNow;
    }
}
