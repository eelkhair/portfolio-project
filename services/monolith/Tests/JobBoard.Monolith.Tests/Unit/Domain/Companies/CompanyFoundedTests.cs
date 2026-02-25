using Shouldly;
using JobBoard.Domain.ValueObjects.Company;

namespace JobBoard.Monolith.Tests.Unit.Domain.Companies;

[Trait("Category", "Unit")]
public class CompanyFoundedTests
{
    [Fact]
    public void Create_WithValidDate_ShouldSucceed()
    {
        var date = new DateTime(2020, 6, 15);

        var result = CompanyFounded.Create(date);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe(date.Date);
    }

    [Fact]
    public void Create_WithNull_ShouldSucceedWithNull()
    {
        var result = CompanyFounded.Create(null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBeNull();
    }

    [Fact]
    public void Create_WithFutureDate_ShouldFail()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);

        var result = CompanyFounded.Create(futureDate);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("CompanyFounded.FutureDate");
    }

    [Fact]
    public void Create_WithDateTooOld_ShouldFail()
    {
        var tooOld = DateTime.UtcNow.AddYears(-301);

        var result = CompanyFounded.Create(tooOld);

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().Code.ShouldBe("CompanyFounded.TooOld");
    }

    [Fact]
    public void Create_StripsTimeComponent()
    {
        var dateWithTime = new DateTime(2020, 6, 15, 14, 30, 0);

        var result = CompanyFounded.Create(dateWithTime);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Value.ShouldBe(new DateTime(2020, 6, 15));
    }
}
