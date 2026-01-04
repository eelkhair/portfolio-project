using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyEEO
{
    public string? Value { get; }
    
    private CompanyEEO(string? value) { Value = value; }
    
    public static Result<CompanyEEO> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyEEO>.Success(new CompanyEEO(value));
        }
        
        value = value.Trim();
        if (value.Length > 500)
        {
            errors.Add(new Error("EEO.TooLong", "EEO cannot exceed 500 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyEEO>.Failure(errors)
            : Result<CompanyEEO>.Success(new CompanyEEO(value));

    }
}