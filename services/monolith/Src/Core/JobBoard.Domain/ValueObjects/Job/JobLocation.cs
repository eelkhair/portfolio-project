using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Job;

public class JobLocation
{
    public string Value { get; }
    
    private JobLocation(string value) { Value = value; }
    
    public static Result<JobLocation> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Location.Empty", "Location cannot be empty."));
        }

        if (value.Length > 150)
        {
            errors.Add(new Error("Location.TooLong", "Location cannot exceed 150 characters."));
        }

        return errors.Count > 0
            ? Result<JobLocation>.Failure(errors)
            : Result<JobLocation>.Success(new JobLocation(value.Trim()));

    }
}