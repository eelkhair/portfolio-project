using JobBoard.AI.Application.Actions.Jobs.Similar;
using JobBoard.AI.Application.Interfaces.Configurations;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetSimilarJobsQueryHandlerTests
{
    [Fact]
    public void Query_DefaultLimit_IsFive()
    {
        var jobId = Guid.NewGuid();
        var query = new GetSimilarJobsQuery(jobId);

        query.JobId.ShouldBe(jobId);
        query.Limit.ShouldBe(5);
    }

    [Fact]
    public void Query_CustomLimit_IsRespected()
    {
        var query = new GetSimilarJobsQuery(Guid.NewGuid(), 10);
        query.Limit.ShouldBe(10);
    }

    [Fact]
    public void Query_ImplementsISystemCommand()
    {
        var query = new GetSimilarJobsQuery(Guid.NewGuid());
        query.ShouldBeAssignableTo<ISystemCommand>();
    }

    [Fact]
    public void JobCandidate_CanBeConstructed()
    {
        var candidate = new JobCandidate
        {
            JobId = Guid.NewGuid(),
            Similarity = 0.85,
            Rank = 1,
            MatchSummary = "Strong match",
            MatchDetails = ["C# expert", "5 years experience"],
            MatchGaps = ["No Kubernetes experience"]
        };

        candidate.Similarity.ShouldBe(0.85);
        candidate.Rank.ShouldBe(1);
        candidate.MatchSummary.ShouldBe("Strong match");
        candidate.MatchDetails!.Count.ShouldBe(2);
        candidate.MatchGaps!.Count.ShouldBe(1);
    }

    [Fact]
    public void JobCandidate_DefaultOptionalFields_AreNull()
    {
        var candidate = new JobCandidate
        {
            JobId = Guid.NewGuid(),
            Similarity = 0.5,
            Rank = 1
        };

        candidate.MatchSummary.ShouldBeNull();
        candidate.MatchDetails.ShouldBeNull();
        candidate.MatchGaps.ShouldBeNull();
    }
}
