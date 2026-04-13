using System.Diagnostics;
using JobBoard.AI.Application.Actions.Jobs.Search;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Persistence;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class SearchJobsQueryHandlerTests
{
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly IAiDbContext _dbContext = Substitute.For<IAiDbContext>();

    public SearchJobsQueryHandlerTests()
    {
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);
    }

    [Fact]
    public void BuildSearchText_WithAllFields_CombinesQueryLocationJobType()
    {
        // Arrange — use reflection to test the private static method
        var query = new SearchJobsQuery("Software Engineer", "New York", "Full-Time");

        // Act
        var method = typeof(SearchJobsQueryHandler)
            .GetMethod("BuildSearchText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [query])!;

        // Assert
        result.ShouldContain("Job Title: Software Engineer");
        result.ShouldContain("Location: New York");
        result.ShouldContain("Job Type: Full-Time");
    }

    [Fact]
    public void BuildSearchText_WithOnlyQuery_ReturnsJobTitleOnly()
    {
        // Arrange
        var query = new SearchJobsQuery("Backend Developer", null, null);

        // Act
        var method = typeof(SearchJobsQueryHandler)
            .GetMethod("BuildSearchText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [query])!;

        // Assert
        result.ShouldContain("Job Title: Backend Developer");
        result.ShouldNotContain("Location:");
        result.ShouldNotContain("Job Type:");
    }

    [Fact]
    public void BuildSearchText_WithNoFields_ReturnsEmptyString()
    {
        // Arrange
        var query = new SearchJobsQuery(null, null, null);

        // Act
        var method = typeof(SearchJobsQueryHandler)
            .GetMethod("BuildSearchText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [query])!;

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void BuildSearchText_WithEmptyStrings_ReturnsEmptyString()
    {
        // Arrange
        var query = new SearchJobsQuery("", "  ", "");

        // Act
        var method = typeof(SearchJobsQueryHandler)
            .GetMethod("BuildSearchText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string)method!.Invoke(null, [query])!;

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EmptySearchText_ReturnsEmptyList()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SearchJobsQuery>>();
        var sut = new SearchJobsQueryHandler(_activityFactory, _embeddingService, logger, _dbContext);
        var query = new SearchJobsQuery(null, null, null);

        // Act
        var result = await sut.HandleAsync(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
        await _embeddingService.DidNotReceive()
            .GenerateEmbeddingsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
