using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.User;

public class UserFirstName
{
    public string Value { get; }

    private UserFirstName(string value) { Value = value; }

    public static Result<UserFirstName> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("FirstName.Empty", "FirstName cannot be empty."));
        }
        if (value.Length > 100)
        {
            errors.Add(new Error("FirstName.TooLong", "FirstName cannot exceed 100 characters."));
        }

        return errors.Count > 0 ? Result<UserFirstName>.Failure(errors) : 
            Result<UserFirstName>.Success(new UserFirstName(value));
    }
}