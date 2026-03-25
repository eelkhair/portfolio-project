using JobBoard.AI.Application.Actions.Resumes.MatchingJobs;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ListMatchingJobsQueryHandlerTests
{
    [Theory]
    [InlineData(0.35, 0.0)]    // floor => 0%
    [InlineData(0.62, 1.0)]    // ceiling => 100%
    [InlineData(0.485, 0.5)]   // midpoint => 50%
    [InlineData(0.20, 0.0)]    // below floor => clamped to 0%
    [InlineData(0.80, 1.0)]    // above ceiling => clamped to 100%
    public void NormalizeScore_VariousInputs_ReturnsExpected(double raw, double expected)
    {
        var result = ListMatchingJobsQueryHandler.NormalizeScore(raw);
        result.ShouldBe(expected, tolerance: 0.01);
    }

    [Fact]
    public void NormalizeScore_AtFloor_ReturnsZero()
    {
        var result = ListMatchingJobsQueryHandler.NormalizeScore(0.35);
        result.ShouldBe(0.0);
    }

    [Fact]
    public void NormalizeScore_AtCeiling_ReturnsOne()
    {
        var result = ListMatchingJobsQueryHandler.NormalizeScore(0.62);
        result.ShouldBe(1.0);
    }

    [Fact]
    public void NormalizeScore_NegativeValue_ReturnsZero()
    {
        var result = ListMatchingJobsQueryHandler.NormalizeScore(-0.5);
        result.ShouldBe(0.0);
    }
}
