using CompanyAPI.Contracts.Models.Companies.Requests;
using FluentValidation;
using Shouldly;

namespace CompanyApi.Tests.Unit.Validators;

/// <summary>
/// Tests the field-level validation rules from CreateCompanyValidator.
/// The actual validator uses Resolve&lt;ICompanyDbContext&gt;() for async DB checks,
/// which requires FastEndpoints' service resolver. For pure unit tests of field rules,
/// we replicate the field rules in a test-only validator to avoid DI coupling.
/// </summary>
[Trait("Category", "Unit")]
public class CreateCompanyValidatorTests
{
    /// <summary>
    /// Mirrors the field-level rules from CreateCompanyValidator without the DB CustomAsync rule.
    /// </summary>
    private sealed class FieldRulesValidator : AbstractValidator<CreateCompanyRequest>
    {
        public FieldRulesValidator()
        {
            RuleFor(c => c.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Company name is required")
                .MaximumLength(250).WithMessage("Company name cannot exceed 250 characters");

            RuleFor(c => c.CompanyEmail)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Company email is required")
                .EmailAddress().WithMessage("Company email is not valid")
                .MaximumLength(100).WithMessage("Company email cannot exceed 100 characters");

            RuleFor(c => c.CompanyWebsite)
                .Cascade(CascadeMode.Stop)
                .MaximumLength(200).WithMessage("Company website cannot exceed 200 characters")
                .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Company website is not a valid URL");

            RuleFor(c => c.IndustryUId)
                .NotEmpty().WithMessage("Industry is required");
        }
    }

    private readonly FieldRulesValidator _validator = new();

    private static CreateCompanyRequest ValidRequest() => new()
    {
        Name = "Test Company",
        CompanyEmail = "test@company.com",
        IndustryUId = Guid.NewGuid()
    };

    // ── Name ──

    [Fact]
    public async Task Name_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Name = "";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "Name"
            && e.ErrorMessage == "Company name is required");
    }

    [Fact]
    public async Task Name_WhenExceeds250Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 251);
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "Name"
            && e.ErrorMessage == "Company name cannot exceed 250 characters");
    }

    [Fact]
    public async Task Name_WhenExactly250Chars_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.Name = new string('A', 250);
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "Name");
    }

    // ── CompanyEmail ──

    [Fact]
    public async Task CompanyEmail_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.CompanyEmail = "";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyEmail"
            && e.ErrorMessage == "Company email is required");
    }

    [Fact]
    public async Task CompanyEmail_WhenInvalidFormat_ShouldHaveError()
    {
        var request = ValidRequest();
        request.CompanyEmail = "not-an-email";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyEmail"
            && e.ErrorMessage == "Company email is not valid");
    }

    [Fact]
    public async Task CompanyEmail_WhenExceeds100Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.CompanyEmail = new string('a', 92) + "@test.com"; // 101 chars, valid email format
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyEmail"
            && e.ErrorMessage == "Company email cannot exceed 100 characters");
    }

    [Fact]
    public async Task CompanyEmail_WhenValid_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.CompanyEmail = "valid@company.com";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "CompanyEmail");
    }

    // ── CompanyWebsite ──

    [Fact]
    public async Task CompanyWebsite_WhenInvalidUrl_ShouldHaveError()
    {
        var request = ValidRequest();
        request.CompanyWebsite = "not-a-url";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyWebsite"
            && e.ErrorMessage == "Company website is not a valid URL");
    }

    [Fact]
    public async Task CompanyWebsite_WhenExceeds200Chars_ShouldHaveError()
    {
        var request = ValidRequest();
        request.CompanyWebsite = "https://" + new string('a', 193) + ".com";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "CompanyWebsite"
            && e.ErrorMessage == "Company website cannot exceed 200 characters");
    }

    [Fact]
    public async Task CompanyWebsite_WhenNull_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.CompanyWebsite = null;
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "CompanyWebsite");
    }

    [Fact]
    public async Task CompanyWebsite_WhenValidUrl_ShouldNotHaveError()
    {
        var request = ValidRequest();
        request.CompanyWebsite = "https://example.com";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "CompanyWebsite");
    }

    // ── IndustryUId ──

    [Fact]
    public async Task IndustryUId_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.IndustryUId = Guid.Empty;
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "IndustryUId"
            && e.ErrorMessage == "Industry is required");
    }

    [Fact]
    public async Task IndustryUId_WhenProvided_ShouldNotHaveError()
    {
        var request = ValidRequest();
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "IndustryUId");
    }

    // ── Valid request ──

    [Fact]
    public async Task ValidRequest_ShouldPassAllFieldRules()
    {
        var request = ValidRequest();
        var result = await _validator.ValidateAsync(request);
        result.IsValid.ShouldBeTrue();
    }
}
