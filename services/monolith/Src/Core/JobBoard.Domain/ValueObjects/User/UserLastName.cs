using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.User;

public class UserLastName
{
    public string Value { get; }

    private UserLastName(string value) { Value = value; }

    public static Result<UserLastName> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("LastName.Empty", "LastName cannot be empty."));
        }
        if (value.Length > 100)
        {
            errors.Add(new Error("LastName.TooLong", "LastName cannot exceed 100 characters."));
        }

        return errors.Count > 0 ? Result<UserLastName>.Failure(errors) : 
            Result<UserLastName>.Success(new UserLastName(value));
    }
}