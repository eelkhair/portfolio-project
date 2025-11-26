using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public sealed class CompanySize
{
    public string? Value { get; }

    private CompanySize(string? value)
    {
        Value = value;
    }

    private const int MaxLength = 30;

    public static Result<CompanySize> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CompanySize>.Success(new CompanySize(null));

        value = value.Trim();

        if (value.Length > MaxLength)
        {
            return Result<CompanySize>.Failure([new Error(
                "CompanySize.TooLong",
                $"Company size cannot exceed {MaxLength} characters."
            )]);
        }

        return Result<CompanySize>.Success(new CompanySize(value));
    }
}