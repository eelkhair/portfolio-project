using System.Text;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.Parse;
using JobBoard.AI.Application.Interfaces.AI;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ParseResumeCommandHandlerTests
{
    private readonly IAiPrompt<ResumeParseRequest> _aiPrompt = Substitute.For<IAiPrompt<ResumeParseRequest>>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly ParseResumeCommandHandler _sut;

    public ParseResumeCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);

        _sut = new ParseResumeCommandHandler(handlerContext, _aiPrompt, _chatService);
    }

    [Fact]
    public async Task HandleAsync_ParsesPlainTextResume()
    {
        // Arrange
        var resumeText = "John Doe\njohn@example.com\n555-123-4567\nSkills: C#, .NET";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(resumeText));

        var request = new ResumeParseRequest
        {
            FileName = "resume.txt",
            ContentType = "text/plain",
            FileContent = base64Content
        };
        var command = new ParseResumeCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("system");
        _aiPrompt.BuildUserPrompt(Arg.Any<ResumeParseRequest>()).Returns("parse {RESUME_TEXT}");
        _aiPrompt.AllowTools.Returns(false);

        var llmResponse = new ResumeParseResponse
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Phone = "555-123-4567",
            Skills = ["C#", ".NET"]
        };

        _chatService.GetResponseAsync<ResumeParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.FirstName.ShouldBe("John");
        result.LastName.ShouldBe("Doe");
        result.Email.ShouldBe("john@example.com");
        result.Skills.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_BackfillsEmail_WhenLlmMissesIt()
    {
        // Arrange
        var resumeText = "John Doe\njohn@example.com\nSenior Developer";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(resumeText));

        var request = new ResumeParseRequest
        {
            FileName = "resume.txt",
            ContentType = "text/plain",
            FileContent = base64Content
        };
        var command = new ParseResumeCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("system");
        _aiPrompt.BuildUserPrompt(Arg.Any<ResumeParseRequest>()).Returns("{RESUME_TEXT}");
        _aiPrompt.AllowTools.Returns(false);

        // LLM returns empty email
        var llmResponse = new ResumeParseResponse
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "", // empty — should be backfilled
            Phone = "555-123-4567"
        };

        _chatService.GetResponseAsync<ResumeParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — regex backfill should find the email
        result.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public async Task HandleAsync_BackfillsPhone_WhenLlmMissesIt()
    {
        // Arrange
        var resumeText = "Jane Smith\n(555) 987-6543\nProject Manager";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(resumeText));

        var request = new ResumeParseRequest
        {
            FileName = "resume.txt",
            ContentType = "text/plain",
            FileContent = base64Content
        };
        var command = new ParseResumeCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("system");
        _aiPrompt.BuildUserPrompt(Arg.Any<ResumeParseRequest>()).Returns("{RESUME_TEXT}");
        _aiPrompt.AllowTools.Returns(false);

        var llmResponse = new ResumeParseResponse
        {
            FirstName = "Jane",
            LastName = "Smith",
            Phone = "" // empty — should be backfilled
        };

        _chatService.GetResponseAsync<ResumeParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(llmResponse);

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Phone.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ReplacesResumeTextPlaceholder()
    {
        // Arrange
        var resumeText = "Simple resume content";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(resumeText));

        var request = new ResumeParseRequest
        {
            FileName = "resume.txt",
            ContentType = "text/plain",
            FileContent = base64Content
        };
        var command = new ParseResumeCommand(request);

        _aiPrompt.BuildSystemPrompt().Returns("system");
        _aiPrompt.BuildUserPrompt(Arg.Any<ResumeParseRequest>()).Returns("Parse this: {RESUME_TEXT}");
        _aiPrompt.AllowTools.Returns(false);

        _chatService.GetResponseAsync<ResumeParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeParseResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — verify the user prompt has the text replaced
        await _chatService.Received(1).GetResponseAsync<ResumeParseResponse>(
            "system",
            Arg.Is<string>(s => s.Contains("Simple resume content") && !s.Contains("{RESUME_TEXT}")),
            false,
            Arg.Any<CancellationToken>());
    }
}
