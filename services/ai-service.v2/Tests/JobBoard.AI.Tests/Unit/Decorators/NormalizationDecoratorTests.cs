using JobBoard.AI.Application.Actions.Drafts.RewriteItem;
using JobBoard.AI.Application.Infrastructure.Decorators;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.AI.Tests.Unit.Decorators;

[Trait("Category", "Unit")]
public class NormalizationDecoratorTests
{
    private readonly IHandler<RewriteItemCommand, RewriteItemResponse> _innerHandler =
        Substitute.For<IHandler<RewriteItemCommand, RewriteItemResponse>>();

    private readonly NormalizationCommandHandlerDecorator<RewriteItemCommand, RewriteItemResponse> _sut;

    public NormalizationDecoratorTests()
    {
        var logger = Substitute.For<ILogger<RewriteItemCommand>>();
        _sut = new NormalizationCommandHandlerDecorator<RewriteItemCommand, RewriteItemResponse>(
            _innerHandler, logger);
    }

    [Fact]
    public async Task HandleAsync_TrimsValue()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "  Untrimmed Title  "
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse { Options = ["Trimmed"] });

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        command.Request.Value.ShouldBe("Untrimmed Title");
    }

    [Fact]
    public async Task HandleAsync_TrimsContextFields()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.AboutRole,
            Value = "test",
            Context = new RewriteItemContext
            {
                Title = "  Job Title  ",
                AboutRole = "  About  ",
                CompanyName = "  Company  ",
                Responsibilities = ["  Resp 1  ", "  ", "  Resp 2  "],
                Qualifications = ["  Qual  ", ""]
            }
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        command.Request.Context!.Title.ShouldBe("Job Title");
        command.Request.Context.AboutRole.ShouldBe("About");
        command.Request.Context.CompanyName.ShouldBe("Company");
        command.Request.Context.Responsibilities!.Count.ShouldBe(2); // empty removed
        command.Request.Context.Qualifications!.Count.ShouldBe(1); // empty removed
    }

    [Fact]
    public async Task HandleAsync_ClampsStyleValues()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Qualifications,
            Value = "test",
            Style = new RewriteItemStyle
            {
                MaxWords = 5, // below min 10
                NumParagraphs = 10, // above max 4
                BulletsPerSection = 1 // below min 3
            }
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        command.Request.Style!.MaxWords.ShouldBe(10); // clamped to min
        command.Request.Style.NumParagraphs.ShouldBe(4); // clamped to max
        command.Request.Style.BulletsPerSection.ShouldBe(3); // clamped to min
    }

    [Fact]
    public async Task HandleAsync_NormalizesLanguageToLowercase()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "test",
            Style = new RewriteItemStyle
            {
                Language = "  EN  "
            }
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        command.Request.Style!.Language.ShouldBe("en");
    }

    [Fact]
    public async Task HandleAsync_DeduplicatesAvoidPhrases()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "test",
            Style = new RewriteItemStyle
            {
                AvoidPhrases = ["  leverage  ", "Leverage", "synergy", "x", "  synergy  "]
            }
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — "x" removed (length <= 1), duplicates removed (case-insensitive)
        var phrases = command.Request.Style!.AvoidPhrases!.ToList();
        phrases.Count.ShouldBe(2); // "leverage" and "synergy" (deduplicated)
    }

    [Fact]
    public async Task HandleAsync_NullStyle_DoesNotThrow()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "test",
            Style = null
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act & Assert — should not throw
        await _sut.HandleAsync(command, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_NullContext_DoesNotThrow()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "test",
            Context = null
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act & Assert
        await _sut.HandleAsync(command, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_NullMaxWords_StaysNull()
    {
        // Arrange
        var request = new RewriteItemRequest
        {
            Field = RewriteField.Title,
            Value = "test",
            Style = new RewriteItemStyle { MaxWords = null }
        };
        var command = new RewriteItemCommand(request);

        _innerHandler.HandleAsync(Arg.Any<RewriteItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RewriteItemResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        command.Request.Style!.MaxWords.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_NonRewriteCommand_CallsInnerDirectly()
    {
        // Use a different decorator with TestCommand to verify non-matching types pass through
        var testInnerHandler = Substitute.For<IHandler<TestCommand, string>>();
        var logger = Substitute.For<ILogger<TestCommand>>();
        var testDecorator = new NormalizationCommandHandlerDecorator<TestCommand, string>(
            testInnerHandler, logger);

        var command = new TestCommand { Input = "  test  " };
        testInnerHandler.HandleAsync(command, Arg.Any<CancellationToken>()).Returns("ok");

        // Act
        var result = await testDecorator.HandleAsync(command, CancellationToken.None);

        // Assert — TestCommand is not RewriteItemCommand, so no normalization
        result.ShouldBe("ok");
        command.Input.ShouldBe("  test  "); // unchanged
    }
}
