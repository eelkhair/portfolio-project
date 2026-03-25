using JobBoard.AI.Domain.AI;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class EmbeddingVectorTests
{
    [Fact]
    public void Constructor_WithValues_StoresValues()
    {
        var values = new float[] { 0.1f, 0.2f, 0.3f };
        var vector = new EmbeddingVector(values);

        vector.Values.ShouldBe(values);
    }

    [Fact]
    public void Dimensions_ReturnsCorrectLength()
    {
        var vector = new EmbeddingVector(new float[1536]);
        vector.Dimensions.ShouldBe(1536);
    }

    [Fact]
    public void Dimensions_EmptyArray_ReturnsZero()
    {
        var vector = new EmbeddingVector([]);
        vector.Dimensions.ShouldBe(0);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var v1 = new EmbeddingVector(new float[] { 1f, 2f, 3f });
        var v2 = new EmbeddingVector(new float[] { 1f, 2f, 3f });

        // Records compare by value for primitives, but arrays use reference equality
        // so two EmbeddingVectors with same values are NOT equal (array reference differs)
        v1.ShouldNotBe(v2);
    }

    [Fact]
    public void Equality_SameReference_AreEqual()
    {
        var values = new float[] { 1f, 2f, 3f };
        var v1 = new EmbeddingVector(values);
        var v2 = new EmbeddingVector(values);

        // Same array reference => record equality holds
        v1.ShouldBe(v2);
    }
}

[Trait("Category", "Unit")]
public class ProviderNameTests
{
    [Fact]
    public void Constructor_SetsValue()
    {
        var provider = new ProviderName("openai");
        provider.Value.ShouldBe("openai");
    }

    [Fact]
    public void OpenAI_StaticProperty_ReturnsCorrectValue()
    {
        ProviderName.OpenAI.Value.ShouldBe("openai");
    }

    [Fact]
    public void Anthropic_StaticProperty_ReturnsCorrectValue()
    {
        ProviderName.Anthropic.Value.ShouldBe("anthropic");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var p1 = new ProviderName("openai");
        var p2 = new ProviderName("openai");
        p1.ShouldBe(p2);
    }

    [Fact]
    public void Equality_DifferentValue_NotEqual()
    {
        var p1 = new ProviderName("openai");
        var p2 = new ProviderName("anthropic");
        p1.ShouldNotBe(p2);
    }
}

[Trait("Category", "Unit")]
public class ModelNameTests
{
    [Fact]
    public void Constructor_SetsValue()
    {
        var model = new ModelName("gpt-4");
        model.Value.ShouldBe("gpt-4");
    }

    [Fact]
    public void Gpt41Mini_StaticProperty_ReturnsCorrectValue()
    {
        ModelName.Gpt41Mini.Value.ShouldBe("gpt-4.1-mini");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var m1 = new ModelName("claude-3");
        var m2 = new ModelName("claude-3");
        m1.ShouldBe(m2);
    }
}
