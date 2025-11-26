using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyPhone
{
    public string? Value { get; }
    
    private CompanyPhone(string? value) { Value = value; }
    
    public static Result<CompanyPhone> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyPhone>.Success(new CompanyPhone(value));
        }
        
        value = value.Trim();
        if (value.Length > 30)
        {
            errors.Add(new Error("Phone.TooLong", "Phone cannot exceed 30 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyPhone>.Failure(errors)
            : Result<CompanyPhone>.Success(new CompanyPhone(value));

    }
}