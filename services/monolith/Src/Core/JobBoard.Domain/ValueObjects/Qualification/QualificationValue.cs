using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Qualification;

public class QualificationValue
{
    public string Value { get; }
    
    private QualificationValue(string value) { Value = value; }
    
    public static Result<QualificationValue> Create(string value)
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
            ? Result<QualificationValue>.Failure(errors)
            : Result<QualificationValue>.Success(new QualificationValue(value.Trim()));

    }
}