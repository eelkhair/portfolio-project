using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.DeleteEmbedding;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeleteResumeEmbeddingCommandHandlerTests
{
    private readonly IAiDbContext _dbContext = Substitute.For<IAiDbContext>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly DeleteResumeEmbeddingCommandHandler _sut;

    public DeleteResumeEmbeddingCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new DeleteResumeEmbeddingCommandHandler(handlerContext, _dbContext, _activityFactory);
    }

    [Fact]
    public void Command_StoresEventData()
    {
        // Arrange
        var resumeUId = Guid.NewGuid();
        var evt = new EventDto<ResumeDeletedV1Event>(
            "user1", "key1",
            new ResumeDeletedV1Event(resumeUId) { UserId = "user1" });

        // Act
        var command = new DeleteResumeEmbeddingCommand(evt);

        // Assert
        command.Event.Data.ResumeUId.ShouldBe(resumeUId);
    }

    [Fact]
    public void Command_ImplementsISystemCommand()
    {
        var evt = new EventDto<ResumeDeletedV1Event>(
            "user1", "key1", new ResumeDeletedV1Event(Guid.NewGuid()) { UserId = "user1" });
        var command = new DeleteResumeEmbeddingCommand(evt);

        command.ShouldBeAssignableTo<ISystemCommand>();
    }
}
