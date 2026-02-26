using FluentValidation.TestHelper;
using JobBoard.Application.Actions.Drafts.Generate;
using JobBoard.Monolith.Contracts.Drafts;

namespace JobBoard.Monolith.Tests.Unit.Application.Validators;

[Trait("Category", "Unit")]
public class GenerateDraftCommandValidatorTests
{
    private readonly GenerateDraftCommandValidator _validator = new();

    private static GenerateDraftCommand CreateValidCommand() => new()
    {
        CompanyId = Guid.NewGuid(),
        Request = new DraftGenRequest
        {
            Brief = "Senior Software Engineer role for cloud platform team",
            RoleLevel = RoleLevel.Senior,
            Tone = Tone.Neutral,
            MaxBullets = 6
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
    public async Task Validate_WithEmptyCompanyId_ShouldFail()
    {
        var command = CreateValidCommand();
        command.CompanyId = Guid.Empty;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.CompanyId)
            .WithErrorMessage("Company ID is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyBrief_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.Brief = "";

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Brief)
            .WithErrorMessage("Brief is required.");
    }

    [Fact]
    public async Task Validate_WithBriefExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.Brief = new string('a', 2001);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.Brief);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    [InlineData(100)]
    public async Task Validate_WithMaxBulletsOutOfRange_ShouldFail(int maxBullets)
    {
        var command = CreateValidCommand();
        command.Request.MaxBullets = maxBullets;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.MaxBullets)
            .WithErrorMessage("MaxBullets must be between 1 and 20.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task Validate_WithMaxBulletsInRange_ShouldPass(int maxBullets)
    {
        var command = CreateValidCommand();
        command.Request.MaxBullets = maxBullets;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Request.MaxBullets);
    }

    [Fact]
    public async Task Validate_WithCompanyNameExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.CompanyName = new string('a', 251);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.CompanyName);
    }

    [Fact]
    public async Task Validate_WithNullCompanyName_ShouldPass()
    {
        var command = CreateValidCommand();
        command.Request.CompanyName = null;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Request.CompanyName);
    }

    [Fact]
    public async Task Validate_WithTitleSeedExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Request.TitleSeed = new string('a', 251);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Request.TitleSeed);
    }

    [Fact]
    public async Task Validate_WithNullTitleSeed_ShouldPass()
    {
        var command = CreateValidCommand();
        command.Request.TitleSeed = null;

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Request.TitleSeed);
    }
}
