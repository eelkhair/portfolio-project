using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyName
{
    public string Value { get; }
    
    private CompanyName(string value) { Value = value; }
    
    public static Result<CompanyName> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Name.Empty", "Name cannot be empty."));
        }

        if (value.Length > 250)
        {
            errors.Add(new Error("Name.TooLong", "Name cannot exceed 250 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyName>.Failure(errors)
            : Result<CompanyName>.Success(new CompanyName(value.Trim()));

    }
}