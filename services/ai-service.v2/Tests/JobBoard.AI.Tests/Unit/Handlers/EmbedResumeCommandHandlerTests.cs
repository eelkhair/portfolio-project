using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.Embed;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using AppUnit = JobBoard.AI.Application.Interfaces.Configurations.Unit;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Application.Interfaces.Persistence;
using JobBoard.AI.Domain.Drafts;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class EmbedResumeCommandHandlerTests
{
    private readonly IMonolithApiClient _monolithClient = Substitute.For<IMonolithApiClient>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly IAiDbContext _dbContext = Substitute.For<IAiDbContext>();
    private readonly IMetricsService _metricsService = Substitute.For<IMetricsService>();
    private readonly IServiceScopeFactory _serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly EmbedResumeCommandHandler _sut;

    public EmbedResumeCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new EmbedResumeCommandHandler(
            handlerContext, _monolithClient, _dbContext, _embeddingService,
            _activityFactory, _serviceScopeFactory, _metricsService);
    }

    [Fact]
    public async Task HandleAsync_NoParsedContent_ReturnsUnitWithoutEmbedding()
    {
        // Arrange
        var resumeUId = Guid.NewGuid();
        var evt = new EventDto<ResumeParsedV1Event>(
            "user1", "key1",
            new ResumeParsedV1Event(resumeUId) { UserId = "user1" });
        var command = new EmbedResumeCommand(evt);

        _monolithClient.GetResumeParsedContentAsync(resumeUId, Arg.Any<CancellationToken>())
            .Returns((ResumeParsedContentResponse?)null);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(AppUnit.Value);
        await _embeddingService.DidNotReceive()
            .GenerateBatchEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithSkillsAndExperience_GeneratesThreeEmbeddings()
    {
        // Arrange
        var resumeUId = Guid.NewGuid();
        var evt = new EventDto<ResumeParsedV1Event>(
            "user1", "key1",
            new ResumeParsedV1Event(resumeUId) { UserId = "user1" });
        var command = new EmbedResumeCommand(evt);

        var parsedContent = new ResumeParsedContentResponse
        {
            FirstName = "John",
            LastName = "Doe",
            Summary = "Experienced developer",
            Skills = ["C#", ".NET", "Azure"],
            WorkHistory =
            [
                new ResumeWorkHistoryDto
                {
                    Company = "Acme Corp",
                    JobTitle = "Senior Developer",
                    Description = "Built microservices"
                }
            ],
            Education = [],
            Certifications = [],
            Projects = []
        };

        _monolithClient.GetResumeParsedContentAsync(resumeUId, Arg.Any<CancellationToken>())
            .Returns(parsedContent);

        // 3 embeddings: full + skills + experience
        var fakeVectors = new List<float[]>
        {
            new float[1536],
            new float[1536],
            new float[1536]
        };
        _embeddingService.GenerateBatchEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(l => l.Count == 3),
                Arg.Any<CancellationToken>())
            .Returns(fakeVectors);

        // Mock DbSet for ResumeEmbeddings — return empty for FirstOrDefaultAsync
        var emptyList = new List<ResumeEmbedding>().AsQueryable();
        var mockDbSet = Substitute.For<DbSet<ResumeEmbedding>>();
        _dbContext.ResumeEmbeddings.Returns(mockDbSet);

        // Mock MatchExplanations DbSet
        var emptyMatchList = new List<MatchExplanation>().AsQueryable();
        var mockMatchDbSet = Substitute.For<DbSet<MatchExplanation>>();
        _dbContext.MatchExplanations.Returns(mockMatchDbSet);

        // Act — handler will throw when it hits FirstOrDefaultAsync on mocked DbSet,
        // but embedding service should have been called before that point
        try
        {
            await _sut.HandleAsync(command, CancellationToken.None);
        }
        catch
        {
            // Expected: DbSet mocking with EF Core async extensions is not supported
        }

        // Assert — embedding service was called with 3 texts (full + skills + experience)
        await _embeddingService.Received(1).GenerateBatchEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(texts =>
                texts.Count == 3 &&
                texts[0].Contains("Experienced developer") &&
                texts[1].Contains("C#") &&
                texts[2].Contains("Senior Developer")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithOnlySummary_GeneratesOneEmbedding()
    {
        // Arrange
        var resumeUId = Guid.NewGuid();
        var evt = new EventDto<ResumeParsedV1Event>(
            "user1", "key1",
            new ResumeParsedV1Event(resumeUId) { UserId = "user1" });
        var command = new EmbedResumeCommand(evt);

        var parsedContent = new ResumeParsedContentResponse
        {
            FirstName = "Jane",
            LastName = "Smith",
            Summary = "Product manager",
            Skills = [],
            WorkHistory = [],
            Education = [],
            Certifications = [],
            Projects = []
        };

        _monolithClient.GetResumeParsedContentAsync(resumeUId, Arg.Any<CancellationToken>())
            .Returns(parsedContent);

        // Only 1 embedding: full (no skills, no experience)
        var fakeVectors = new List<float[]> { new float[1536] };
        _embeddingService.GenerateBatchEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(l => l.Count == 1),
                Arg.Any<CancellationToken>())
            .Returns(fakeVectors);

        var mockDbSet = Substitute.For<DbSet<ResumeEmbedding>>();
        _dbContext.ResumeEmbeddings.Returns(mockDbSet);

        var mockMatchDbSet = Substitute.For<DbSet<MatchExplanation>>();
        _dbContext.MatchExplanations.Returns(mockMatchDbSet);

        // Act — handler will throw when it hits FirstOrDefaultAsync on mocked DbSet,
        // but embedding service should have been called before that point
        try
        {
            await _sut.HandleAsync(command, CancellationToken.None);
        }
        catch
        {
            // Expected: DbSet mocking with EF Core async extensions is not supported
        }

        // Assert — embedding service was called with 1 text (full only, no skills/experience)
        await _embeddingService.Received(1).GenerateBatchEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void BuildSectionTexts_WithFullContent_BuildsAllSections()
    {
        // Arrange
        var content = new ResumeParsedContentResponse
        {
            Summary = "Full-stack developer",
            Skills = ["React", "Node.js", "TypeScript"],
            WorkHistory =
            [
                new ResumeWorkHistoryDto
                {
                    Company = "Tech Co",
                    JobTitle = "Lead Developer",
                    Description = "Led team of 5"
                }
            ],
            Education =
            [
                new ResumeEducationDto
                {
                    Institution = "MIT",
                    Degree = "BS",
                    FieldOfStudy = "Computer Science"
                }
            ],
            Certifications =
            [
                new ResumeCertificationDto
                {
                    Name = "AWS Solutions Architect",
                    IssuingOrganization = "Amazon"
                }
            ],
            Projects =
            [
                new ResumeProjectDto
                {
                    Name = "OpenSource Tool",
                    Description = "CLI tool for devs",
                    Technologies = ["Go", "gRPC"]
                }
            ]
        };

        // Act — test via reflection since BuildSectionTexts is private
        var method = typeof(EmbedResumeCommandHandler)
            .GetMethod("BuildSectionTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, [content])!;

        // Use dynamic to access the record properties
        var fullProp = result.GetType().GetProperty("Full")!;
        var skillsProp = result.GetType().GetProperty("Skills")!;
        var experienceProp = result.GetType().GetProperty("Experience")!;

        var fullText = (string)fullProp.GetValue(result)!;
        var skillsText = (string?)skillsProp.GetValue(result);
        var experienceText = (string?)experienceProp.GetValue(result);

        // Assert
        fullText.ShouldContain("Full-stack developer");
        fullText.ShouldContain("React");
        fullText.ShouldContain("Lead Developer");
        fullText.ShouldContain("MIT");
        fullText.ShouldContain("AWS Solutions Architect");
        fullText.ShouldContain("OpenSource Tool");

        skillsText.ShouldNotBeNull();
        skillsText.ShouldContain("React");
        skillsText.ShouldContain("Node.js");

        experienceText.ShouldNotBeNull();
        experienceText.ShouldContain("Lead Developer");
        experienceText.ShouldContain("Tech Co");
    }

    [Fact]
    public void BuildSectionTexts_WithNoSkillsOrExperience_ReturnsNullForOptionalSections()
    {
        // Arrange
        var content = new ResumeParsedContentResponse
        {
            Summary = "Manager",
            Skills = [],
            WorkHistory = [],
            Education = [],
            Certifications = [],
            Projects = []
        };

        // Act
        var method = typeof(EmbedResumeCommandHandler)
            .GetMethod("BuildSectionTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = method!.Invoke(null, [content])!;

        var skillsProp = result.GetType().GetProperty("Skills")!;
        var experienceProp = result.GetType().GetProperty("Experience")!;

        var skillsText = (string?)skillsProp.GetValue(result);
        var experienceText = (string?)experienceProp.GetValue(result);

        // Assert
        skillsText.ShouldBeNull();
        experienceText.ShouldBeNull();
    }
}
