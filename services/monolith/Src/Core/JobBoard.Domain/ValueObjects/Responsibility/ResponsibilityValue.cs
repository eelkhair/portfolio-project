using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Responsibility;

public class ResponsibilityValue
{
    public string Value { get; }
    
    private ResponsibilityValue(string value) { Value = value; }
    
    public static Result<ResponsibilityValue> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Value.Empty", "Value cannot be empty."));
        }

        if (value.Length > 250)
        {
            errors.Add(new Error("Value.TooLong", "Value cannot exceed 250 characters."));
        }

        return errors.Count > 0
            ? Result<ResponsibilityValue>.Failure(errors)
            : Result<ResponsibilityValue>.Success(new ResponsibilityValue(value.Trim()));

    }
}