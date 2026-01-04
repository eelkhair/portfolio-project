using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyWebsite
{
    public string? Value { get; }
    
    private CompanyWebsite(string? value) { Value = value; }
    
    public static Result<CompanyWebsite> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyWebsite>.Success(new CompanyWebsite(value));
        }
        
        value = value.Trim();
        if (value.Length > 200)
        {
            errors.Add(new Error("Website.TooLong", "Website cannot exceed 200 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyWebsite>.Failure(errors)
            : Result<CompanyWebsite>.Success(new CompanyWebsite(value));

    }
}