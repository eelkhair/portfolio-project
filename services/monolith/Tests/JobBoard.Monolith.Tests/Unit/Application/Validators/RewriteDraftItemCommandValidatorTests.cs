using FluentValidation.TestHelper;
using JobBoard.Application.Actions.Jobs.Drafts;
using JobBoard.Monolith.Contracts.Drafts;

namespace JobBoard.Monolith.Tests.Unit.Application.Validators;

[Trait("Category", "Unit")]
public class RewriteDraftItemCommandValidatorTests
{
    private readonly RewriteDraftItemCommandValidator _validator = new();

    private static RewriteDraftItemCommand CreateValidCommand() => new()
    {
        DraftItemRewriteRequest = new DraftItemRewriteRequest
        {
            Field = "title",
            Value = "Senior Software Engineer",
            Context = new Dictionary<string, object> { ["role"] = "Engineer" },
            Style = new Dictionary<string, object> { ["tone"] = "professional" }
        }
    };

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var command = CreateValidCommand();

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyField_ShouldFail()
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Field = "";

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Field)
            .WithErrorMessage("Field is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyValue_ShouldFail()
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Value = "";

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Value)
            .WithErrorMessage("Value is required.");
    }

    [Fact]
    public async Task Validate_WithFieldExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Field = new string('a', 101);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Field);
    }

    [Fact]
    public async Task Validate_WithValueExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Value = new string('a', 5001);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Value);
    }

    [Fact]
    public async Task Validate_WithNullContext_ShouldFail()
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Context = null!;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Context)
            .WithErrorMessage("Context is required.");
    }

    [Fact]
    public async Task Validate_WithNullStyle_ShouldFail()
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Style = null!;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Style)
            .WithErrorMessage("Style is required.");
    }

    [Theory]
    [InlineData("title")]
    [InlineData("aboutRole")]
    [InlineData("responsibilities")]
    [InlineData("qualifications")]
    public async Task Validate_WithVariousValidFields_ShouldPass(string field)
    {
        var command = CreateValidCommand();
        command.DraftItemRewriteRequest.Field = field;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DraftItemRewriteRequest.Field);
    }
}
