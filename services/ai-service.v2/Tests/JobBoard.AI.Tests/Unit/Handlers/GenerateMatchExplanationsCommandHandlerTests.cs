using JobBoard.AI.Application.Actions.Resumes.MatchExplanations;
using JobBoard.AI.Application.Interfaces.Configurations;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GenerateMatchExplanationsCommandHandlerTests
{
    [Fact]
    public void Command_SetsProperties()
    {
        var resumeUId = Guid.NewGuid();
        var command = new GenerateMatchExplanationsCommand(resumeUId, "user-123");

        command.ResumeUId.ShouldBe(resumeUId);
        command.UserId.ShouldBe("user-123");
    }

    [Fact]
    public void Command_NullUserId_DefaultsToNull()
    {
        var command = new GenerateMatchExplanationsCommand(Guid.NewGuid());
        command.UserId.ShouldBeNull();
    }

    [Fact]
    public void Command_ImplementsISystemCommand()
    {
        var command = new GenerateMatchExplanationsCommand(Guid.NewGuid());
        command.ShouldBeAssignableTo<ISystemCommand>();
    }

    [Fact]
    public void MatchExplanationLlmResponse_DefaultExplanations_Empty()
    {
        var response = new MatchExplanationLlmResponse();
        response.Explanations.ShouldBeEmpty();
    }

    [Fact]
    public void JobExplanationItem_CanBeConstructed()
    {
        var item = new JobExplanationItem
        {
            JobId = Guid.NewGuid(),
            Summary = "Good match for your skills",
            Details = ["C# expertise matches", "Cloud experience aligns"],
            Gaps = ["Missing Kubernetes"]
        };

        item.Summary.ShouldNotBeEmpty();
        item.Details.Count.ShouldBe(2);
        item.Gaps.Count.ShouldBe(1);
    }
}
