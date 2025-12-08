

using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyExternalId
{
    public string? Value { get; }
    
    private CompanyExternalId(string? value) { Value = value; }
    
    public static Result<CompanyExternalId> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyExternalId>.Success(new CompanyExternalId(null));
        }
        
        value = value.Trim();
        
        if (value.Length > 50)
        {
            errors.Add(new Error("ExternalId.TooLong", "ExternalId cannot exceed 50 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyExternalId>.Failure(errors)
            : Result<CompanyExternalId>.Success(new CompanyExternalId(value));

    }
}