using JobBoard.Domain.Exceptions;
using JobBoard.Domain.Helpers;
using JobBoard.Domain.ValueObjects.Company;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Domain.Shared;

[Trait("Category", "Unit")]
public class ResultExtensionsTests
{
    [Fact]
    public void Collect_WhenSuccess_ShouldReturnValueAndNotAddErrors()
    {
        var errors = new List<Error>();
        var result = CompanyName.Create("ValidName");

        var value = result.Collect<CompanyName, string>(errors);

        value.ShouldBe("ValidName");
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Collect_WhenFailure_ShouldReturnDefaultAndAddErrors()
    {
        var errors = new List<Error>();
        var result = CompanyName.Create("");

        var value = result.Collect<CompanyName, string>(errors);

        value.ShouldBeNull();
        errors.ShouldNotBeEmpty();
        errors.ShouldContain(e => e.Code == "Name.Empty");
    }

    [Fact]
    public void Collect_WhenMultipleFailures_ShouldAccumulateErrors()
    {
        var errors = new List<Error>();

        CompanyName.Create("").Collect<CompanyName, string>(errors);
        CompanyEmail.Create("").Collect<CompanyEmail, string>(errors);

        errors.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Collect_ShouldNotClearExistingErrors()
    {
        var errors = new List<Error> { new("Existing", "Pre-existing error") };

        CompanyName.Create("").Collect<CompanyName, string>(errors);

        errors.ShouldContain(e => e.Code == "Existing");
        errors.Count.ShouldBeGreaterThan(1);
    }
}
