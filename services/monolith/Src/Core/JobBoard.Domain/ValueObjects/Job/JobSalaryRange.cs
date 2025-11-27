using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Job;

public class JobSalaryRange
{
    public string? Value { get; }
    
    private JobSalaryRange(string? value) { Value = value; }
    
    public static Result<JobSalaryRange> Create(string? value)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<JobSalaryRange>.Success(new JobSalaryRange(value));
        }
        
        value = value.Trim();
        if (value.Length > 100)
        {
            errors.Add(new Error("SalaryRange.TooLong", "Salary Range cannot exceed 100 characters."));
        }

        return errors.Count > 0
            ? Result<JobSalaryRange>.Failure(errors)
            : Result<JobSalaryRange>.Success(new JobSalaryRange(value));

    }
}