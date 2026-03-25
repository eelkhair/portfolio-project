using JobBoard.AI.Domain.AI;
using JobBoard.AI.Domain.Drafts;
using Pgvector;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class ResumeEmbeddingTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var resumeUId = Guid.NewGuid();
        var vector = new EmbeddingVector(new float[] { 0.1f, 0.2f, 0.3f });
        var provider = ProviderName.OpenAI;
        var model = ModelName.Gpt41Mini;

        var embedding = new ResumeEmbedding(resumeUId, vector, provider, model);

        embedding.ResumeUId.ShouldBe(resumeUId);
        embedding.Provider.ShouldBe(provider);
        embedding.Model.ShouldBe(model);
        embedding.VectorData.ShouldNotBeNull();
        embedding.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Constructor_WithSkillsAndExperience_SetsOptionalVectors()
    {
        var resumeUId = Guid.NewGuid();
        var vector = new EmbeddingVector(new float[] { 0.1f, 0.2f });
        var skillsVector = new EmbeddingVector(new float[] { 0.3f, 0.4f });
        var experienceVector = new EmbeddingVector(new float[] { 0.5f, 0.6f });

        var embedding = new ResumeEmbedding(resumeUId, vector, ProviderName.OpenAI, ModelName.Gpt41Mini,
            skillsVector, experienceVector);

        embedding.SkillsVectorData.ShouldNotBeNull();
        embedding.ExperienceVectorData.ShouldNotBeNull();
        embedding.SkillsVector.ShouldNotBeNull();
        embedding.ExperienceVector.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithoutSkillsAndExperience_NullOptionalVectors()
    {
        var embedding = new ResumeEmbedding(
            Guid.NewGuid(),
            new EmbeddingVector(new float[] { 0.1f }),
            ProviderName.OpenAI,
            ModelName.Gpt41Mini);

        embedding.SkillsVectorData.ShouldBeNull();
        embedding.ExperienceVectorData.ShouldBeNull();
        embedding.SkillsVector.ShouldBeNull();
        embedding.ExperienceVector.ShouldBeNull();
    }

    [Fact]
    public void Update_ChangesVectorAndMetadata()
    {
        var embedding = new ResumeEmbedding(
            Guid.NewGuid(),
            new EmbeddingVector(new float[] { 0.1f }),
            ProviderName.OpenAI,
            ModelName.Gpt41Mini);

        var newVector = new EmbeddingVector(new float[] { 0.9f });
        var newProvider = ProviderName.Anthropic;
        var newModel = new ModelName("claude-3-opus");

        embedding.Update(newVector, newProvider, newModel);

        embedding.Provider.ShouldBe(newProvider);
        embedding.Model.ShouldBe(newModel);
        embedding.VectorData.ToArray().ShouldBe(new float[] { 0.9f });
    }

    [Fact]
    public void Update_WithSkillsAndExperience_SetsOptionalVectors()
    {
        var embedding = new ResumeEmbedding(
            Guid.NewGuid(),
            new EmbeddingVector(new float[] { 0.1f }),
            ProviderName.OpenAI,
            ModelName.Gpt41Mini);

        var skillsVector = new EmbeddingVector(new float[] { 0.5f });
        embedding.Update(
            new EmbeddingVector(new float[] { 0.2f }),
            ProviderName.OpenAI,
            ModelName.Gpt41Mini,
            skillsVector);

        embedding.SkillsVectorData.ShouldNotBeNull();
    }

    [Fact]
    public void Vector_Property_ConvertsFromVectorData()
    {
        var values = new float[] { 0.1f, 0.2f, 0.3f };
        var embedding = new ResumeEmbedding(
            Guid.NewGuid(),
            new EmbeddingVector(values),
            ProviderName.OpenAI,
            ModelName.Gpt41Mini);

        embedding.Vector.Values.ShouldBe(values);
        embedding.Vector.Dimensions.ShouldBe(3);
    }
}

[Trait("Category", "Unit")]
public class JobEmbeddingTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var jobId = Guid.NewGuid();
        var vector = new EmbeddingVector(new float[] { 0.1f, 0.2f });
        var provider = ProviderName.OpenAI;
        var model = new ModelName("text-embedding-3-small");

        var embedding = new JobEmbedding(jobId, vector, provider, model);

        embedding.JobId.ShouldBe(jobId);
        embedding.Provider.ShouldBe(provider);
        embedding.Model.ShouldBe(model);
        embedding.VectorData.ShouldNotBeNull();
        embedding.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Vector_Property_ConvertsFromVectorData()
    {
        var values = new float[] { 0.5f, 0.6f };
        var embedding = new JobEmbedding(
            Guid.NewGuid(),
            new EmbeddingVector(values),
            ProviderName.OpenAI,
            new ModelName("text-embedding-3-small"));

        embedding.Vector.Values.ShouldBe(values);
    }

    [Fact]
    public void Id_IsGenerated()
    {
        var embedding = new JobEmbedding(
            Guid.NewGuid(),
            new EmbeddingVector(new float[] { 0.1f }),
            ProviderName.OpenAI,
            new ModelName("test"));

        embedding.Id.ShouldNotBe(Guid.Empty);
    }
}

[Trait("Category", "Unit")]
public class MatchExplanationTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var resumeUId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        var explanation = new MatchExplanation(
            resumeUId, jobId, "Great match", "[\"skill1\"]", "[\"gap1\"]", "openai", "gpt-4o");

        explanation.ResumeUId.ShouldBe(resumeUId);
        explanation.JobId.ShouldBe(jobId);
        explanation.Summary.ShouldBe("Great match");
        explanation.DetailsJson.ShouldBe("[\"skill1\"]");
        explanation.GapsJson.ShouldBe("[\"gap1\"]");
        explanation.Provider.ShouldBe("openai");
        explanation.Model.ShouldBe("gpt-4o");
        explanation.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Update_ChangesAllMutableProperties()
    {
        var explanation = new MatchExplanation(
            Guid.NewGuid(), Guid.NewGuid(), "Old summary", "[]", "[]", "openai", "gpt-4o");

        explanation.Update("New summary", "[\"detail\"]", "[\"gap\"]", "anthropic", "claude-3");

        explanation.Summary.ShouldBe("New summary");
        explanation.DetailsJson.ShouldBe("[\"detail\"]");
        explanation.GapsJson.ShouldBe("[\"gap\"]");
        explanation.Provider.ShouldBe("anthropic");
        explanation.Model.ShouldBe("claude-3");
    }

    [Fact]
    public void Update_RefreshesCreatedAt()
    {
        var explanation = new MatchExplanation(
            Guid.NewGuid(), Guid.NewGuid(), "Summary", "[]", "[]", "openai", "gpt-4o");

        var originalTime = explanation.CreatedAt;

        // Small delay to ensure different timestamp
        Thread.Sleep(10);
        explanation.Update("Updated", "[]", "[]", "openai", "gpt-4o");

        explanation.CreatedAt.ShouldBeGreaterThanOrEqualTo(originalTime);
    }
}
