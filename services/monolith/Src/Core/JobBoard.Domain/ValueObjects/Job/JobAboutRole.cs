using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Job;

public class JobAboutRole
{
    public string Value { get; }
    
    private JobAboutRole(string value) { Value = value; }
    
    public static Result<JobAboutRole> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("AboutRole.Empty", "AboutRole cannot be empty."));
        }

        if (value.Length > 3000)
        {
            errors.Add(new Error("AboutRole.TooLong", "AboutRole cannot exceed 3000 characters."));
        }

        return errors.Count > 0
            ? Result<JobAboutRole>.Failure(errors)
            : Result<JobAboutRole>.Success(new JobAboutRole(value.Trim()));

    }
}