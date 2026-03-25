using JobAPI.Contracts.Models.Jobs.Requests;
using JobAPI.Contracts.Enums;
using FluentValidation;
using Shouldly;

namespace JobApi.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public class CreateJobValidatorTests
{
    private sealed class FieldRulesValidator : AbstractValidator<CreateJobRequest>
    {
        public FieldRulesValidator()
        {
            RuleFor(c => c.Title).NotEmpty();
            RuleFor(c => c.AboutRole).NotEmpty();
        }
    }

    private readonly FieldRulesValidator _validator = new();

    private static CreateJobRequest ValidRequest() => new()
    {
        Title = "Software Engineer",
        AboutRole = "Build great software",
        CompanyUId = Guid.NewGuid(),
        Location = "Remote",
        JobType = JobType.FullTime
    };

    [Fact]
    public async Task Title_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.Title = "";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "Title");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Title_WhenNullOrEmpty_ShouldHaveError(string? title)
    {
        var request = ValidRequest();
        request.Title = title!;
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Title_WhenProvided_ShouldNotHaveError()
    {
        var request = ValidRequest();
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task AboutRole_WhenEmpty_ShouldHaveError()
    {
        var request = ValidRequest();
        request.AboutRole = "";
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "AboutRole");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AboutRole_WhenNullOrEmpty_ShouldHaveError(string? aboutRole)
    {
        var request = ValidRequest();
        request.AboutRole = aboutRole!;
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldContain(e => e.PropertyName == "AboutRole");
    }

    [Fact]
    public async Task AboutRole_WhenProvided_ShouldNotHaveFieldError()
    {
        var request = ValidRequest();
        var result = await _validator.ValidateAsync(request);
        result.Errors.ShouldNotContain(e => e.PropertyName == "AboutRole");
    }

    [Fact]
    public async Task ValidRequest_ShouldPassAllFieldRules()
    {
        var request = ValidRequest();
        var result = await _validator.ValidateAsync(request);
        result.IsValid.ShouldBeTrue();
    }
}
