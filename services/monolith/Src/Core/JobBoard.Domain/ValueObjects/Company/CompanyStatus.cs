using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyStatus
{
    public string Value { get; }
    
    private CompanyStatus(string value) { Value = value; }
    
    public static Result<CompanyStatus> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Status.Empty", "Status cannot be empty."));
        }

        if (value.Length > 30)
        {
            errors.Add(new Error("Status.TooLong", "Status cannot exceed 30 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyStatus>.Failure(errors)
            : Result<CompanyStatus>.Success(new CompanyStatus(value.Trim()));

    }
}