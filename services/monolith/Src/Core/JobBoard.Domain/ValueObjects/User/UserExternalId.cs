using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.User;

public class UserExternalId
{
    public string? Value { get; }

    private UserExternalId(string? value) { Value = value; }

    public static Result<UserExternalId> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<UserExternalId>.Success(new UserExternalId(null));
        }
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("UserExternalId.Empty", "UserExternalId cannot be empty."));
        }
        if (value.Length > 100)
        {
            errors.Add(new Error("UserExternalId.TooLong", "UserExternalId cannot exceed 100 characters."));
        }

        return errors.Count > 0 ? Result<UserExternalId>.Failure(errors) : 
            Result<UserExternalId>.Success(new UserExternalId(value.Trim()));
    }
}