using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Job;

public class JobTitle
{
    public string Value { get; }
    
    private JobTitle(string value) { Value = value; }
    
    public static Result<JobTitle> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Title.Empty", "Title cannot be empty."));
        }

        if (value.Length > 250)
        {
            errors.Add(new Error("Title.TooLong", "Title cannot exceed 250 characters."));
        }

        return errors.Count > 0
            ? Result<JobTitle>.Failure(errors)
            : Result<JobTitle>.Success(new JobTitle(value.Trim()));

    }
}