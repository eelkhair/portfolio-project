using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public sealed class CompanyFounded
{
    public DateTime? Value { get; }

    private CompanyFounded(DateTime? value)
    {
        Value = value;
    }

    public static Result<CompanyFounded> Create(DateTime? value)
    {
        var errors = new List<Error>();

        if (value is null)
            return Result<CompanyFounded>.Success(new CompanyFounded(null));

        if (value > DateTime.UtcNow)
        {
            errors.Add(new Error(
                "CompanyFounded.FutureDate",
                "Founded date cannot be in the future."));
        }

        var minDate = DateTime.UtcNow.AddYears(-300);
        if (value < minDate)
        {
            errors.Add(new Error(
                "CompanyFounded.TooOld",
                "Founded date is not realistic."));
        }

        return errors.Any()
            ? Result<CompanyFounded>.Failure(errors)
            : Result<CompanyFounded>.Success(new CompanyFounded(value.Value.Date));
    }
}