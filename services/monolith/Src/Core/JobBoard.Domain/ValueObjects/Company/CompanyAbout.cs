using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public class CompanyAbout
{
    public string? Value { get; }
    
    private CompanyAbout(string? value) { Value = value; }
    
    public static Result<CompanyAbout> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyAbout>.Success(new CompanyAbout(null));
        }
        
        value = value.Trim();
        
        if (value.Length > 2000)
        {
            errors.Add(new Error("About.TooLong", "About cannot exceed 2000 characters."));
        }

        return errors.Count > 0
            ? Result<CompanyAbout>.Failure(errors)
            : Result<CompanyAbout>.Success(new CompanyAbout(value));

    }
}