using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyDescription
{
    public string? Value { get; }
    
    private CompanyDescription(string? value) { Value = value; }
    
    public static Result<CompanyDescription> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyDescription>.Success(new CompanyDescription(null));
        }
        
        value = value.Trim();
        
        if (value.Length > 4000)
        {
            errors.Add(new Error("Description.TooLong", "Description cannot exceed 4000 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyDescription>.Failure(errors)
            : Result<CompanyDescription>.Success(new CompanyDescription(value));

    }
}