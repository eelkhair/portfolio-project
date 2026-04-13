using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Jobs.Publish;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class PublishJobCommandHandlerTests
{
    private readonly IAiDbContext _dbContext = Substitute.For<IAiDbContext>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly PublishJobCommandHandler _sut;

    public PublishJobCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new PublishJobCommandHandler(handlerContext, _dbContext, _embeddingService, _activityFactory);
    }

    [Fact]
    public async Task HandleAsync_GeneratesEmbeddingFromJobData()
    {
        // Arrange
        var jobEvent = new PublishedJobEvent
        {
            UId = Guid.NewGuid(),
            Title = "Senior Developer",
            Location = "Remote",
            JobType = "Full-time",
            AboutRole = "Build great software",
            Responsibilities = ["Write code", "Review PRs"],
            Qualifications = ["5 years C#", "AWS experience"]
        };

        var evt = new EventDto<PublishedJobEvent>("user1", "key1", jobEvent);
        var command = new PublishJobCommand(evt);

        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[1536]);

        var mockJobSet = Substitute.For<DbSet<JobEmbedding>>();
        _dbContext.JobEmbeddings.Returns(mockJobSet);
        var mockMatchSet = Substitute.For<DbSet<MatchExplanation>>();
        _dbContext.MatchExplanations.Returns(mockMatchSet);

        // Act — will throw on FirstOrDefaultAsync but embedding service should be called first
        try
        {
            await _sut.HandleAsync(command, CancellationToken.None);
        }
        catch
        {
            // Expected: DbSet mocking with EF Core async extensions is not supported
        }

        // Assert
        await _embeddingService.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<string>(s =>
                s.Contains("Senior Developer") &&
                s.Contains("Remote") &&
                s.Contains("Build great software") &&
                s.Contains("Write code") &&
                s.Contains("5 years C#")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void PublishedJobEvent_CanBeConstructed()
    {
        var evt = new PublishedJobEvent
        {
            UId = Guid.NewGuid(),
            Title = "Test Job",
            CompanyUId = Guid.NewGuid(),
            CompanyName = "Test Corp",
            Location = "NYC",
            JobType = "Contract",
            AboutRole = "Do things",
            Responsibilities = ["Resp 1"],
            Qualifications = ["Qual 1"]
        };

        evt.Title.ShouldBe("Test Job");
        evt.Responsibilities.Count.ShouldBe(1);
    }
}
