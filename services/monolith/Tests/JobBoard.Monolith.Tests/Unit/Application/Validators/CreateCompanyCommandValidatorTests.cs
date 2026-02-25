using FluentValidation;
using FluentValidation.TestHelper;
using JobBoard.Application.Actions.Companies.Create;
using JobBoard.Application.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Validators;

/// <summary>
/// Tests synchronous FluentValidation rules only.
/// Async custom rules (uniqueness/existence checks) require a real EF temporal table
/// DbContext and are covered by integration tests.
/// </summary>
[Trait("Category", "Unit")]
public class CreateCompanyCommandValidatorTests
{
    private readonly CreateCompanyCommandValidator _validator;

    public CreateCompanyCommandValidatorTests()
    {
        var dbContext = Substitute.For<IJobBoardDbContext>();
        var logger = Substitute.For<ILogger<CreateCompanyCommandValidator>>();
        _validator = new CreateCompanyCommandValidator(dbContext, logger);
    }

    private static CreateCompanyCommand CreateValidCommand() => new()
    {
        Name = "Acme Corp",
        CompanyEmail = "info@acme.com",
        CompanyWebsite = "https://acme.com",
        IndustryUId = Guid.NewGuid(),
        AdminFirstName = "John",
        AdminLastName = "Doe",
        AdminEmail = "john@acme.com"
    };

    [Fact]
    public async Task Validate_WithEmptyName_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Name = "";

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.Name));

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Company name is required.");
    }

    [Fact]
    public async Task Validate_WithNameExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.Name = new string('A', 251);

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.Name));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithValidName_ShouldPass()
    {
        var command = CreateValidCommand();

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.Name));

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithEmptyCompanyEmail_ShouldFail()
    {
        var command = CreateValidCommand();
        command.CompanyEmail = "";

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.CompanyEmail));

        result.ShouldHaveValidationErrorFor(x => x.CompanyEmail);
    }

    [Fact]
    public async Task Validate_WithInvalidCompanyEmail_ShouldFail()
    {
        var command = CreateValidCommand();
        command.CompanyEmail = "not-an-email";

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.CompanyEmail));

        result.ShouldHaveValidationErrorFor(x => x.CompanyEmail);
    }

    [Fact]
    public async Task Validate_WithCompanyEmailExceedingMaxLength_ShouldFail()
    {
        var command = CreateValidCommand();
        command.CompanyEmail = new string('a', 92) + "@test.com";

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.CompanyEmail));

        result.ShouldHaveValidationErrorFor(x => x.CompanyEmail);
    }

    [Fact]
    public async Task Validate_WithInvalidCompanyWebsite_ShouldFail()
    {
        var command = CreateValidCommand();
        command.CompanyWebsite = "not-a-url";

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.CompanyWebsite));

        result.ShouldHaveValidationErrorFor(x => x.CompanyWebsite)
            .WithErrorMessage("Company website is not a valid URL");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithNullOrEmptyWebsite_ShouldPass(string? website)
    {
        var command = CreateValidCommand();
        command.CompanyWebsite = website;

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.CompanyWebsite));

        result.ShouldNotHaveValidationErrorFor(x => x.CompanyWebsite);
    }

    [Fact]
    public async Task Validate_WithEmptyIndustryUId_ShouldFail()
    {
        var command = CreateValidCommand();
        command.IndustryUId = Guid.Empty;

        var result = await _validator.TestValidateAsync(command,
            o => o.IncludeProperties(x => x.IndustryUId));

        result.ShouldHaveValidationErrorFor(x => x.IndustryUId)
            .WithErrorMessage("Industry is required");
    }
}
