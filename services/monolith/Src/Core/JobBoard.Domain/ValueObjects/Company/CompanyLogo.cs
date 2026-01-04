using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyLogo
{
    public string? Value { get; }
    
    private CompanyLogo(string? value) { Value = value; }
    
    public static Result<CompanyLogo> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyLogo>.Success(new CompanyLogo(value));
        }
        
        value = value.Trim();
        if (value.Length > 400)
        {
            errors.Add(new Error("Logo.TooLong", "Logo cannot exceed 400 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyLogo>.Failure(errors)
            : Result<CompanyLogo>.Success(new CompanyLogo(value));

    }
}