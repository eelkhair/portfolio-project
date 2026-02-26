using FluentValidation.TestHelper;
using JobBoard.Application.Actions.Settings.Provider;
using JobBoard.Monolith.Contracts.Settings;

namespace JobBoard.Monolith.Tests.Unit.Application.Validators;

[Trait("Category", "Unit")]
public class UpdateProviderCommandValidatorTests
{
    private readonly UpdateProviderCommandValidator _validator = new();

    private static UpdateProviderCommand CreateValidCommand() => new()
    {
        Request = new UpdateProviderRequest
        {
            Provider = "openai",
            Model = "gpt-4.1-mini"
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
    public async Task Validate_WithEmptyProvider_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.Provider = "";

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Provider)
            .WithErrorMessage("Provider is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyModel_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.Model = "";

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Model)
            .WithErrorMessage("Model is required.");
    }

    [Fact]
    public async Task Validate_WithProviderExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.Provider = new string('a', 51);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Provider);
    }

    [Fact]
    public async Task Validate_WithModelExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.Model = new string('a', 101);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Model);
    }

    [Theory]
    [InlineData("openai", "gpt-4.1-mini")]
    [InlineData("anthropic", "claude-sonnet-4-5-20250929")]
    [InlineData("azure", "gpt-4o")]
    public async Task Validate_WithVariousValidProviders_ShouldPass(string provider, string model)
    {
        var command = CreateValidCommand();
        command.Request.Provider = provider;
        command.Request.Model = model;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
