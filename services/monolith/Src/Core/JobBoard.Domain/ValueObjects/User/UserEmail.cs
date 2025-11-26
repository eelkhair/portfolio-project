using System.Text.RegularExpressions;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.User;

public partial class UserEmail
{
    public string Value { get; }
    private static readonly Regex FormatRegex = MyRegex();

    private UserEmail(string value) { Value = value; }

    public static Result<UserEmail> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Email.Empty", "Email cannot be empty."));
        }
        if (value.Length > 256)
        {
            errors.Add(new Error("Email.TooLong", "Email cannot exceed 256 characters."));
        }
        if (!string.IsNullOrWhiteSpace(value) && !FormatRegex.IsMatch(value))
        {
            errors.Add(new Error("Email.InvalidFormat", "Email is not in a valid format."));
        }
        return errors.Count > 0 ? Result<UserEmail>.Failure(errors) : 
            Result<UserEmail>.Success(new UserEmail(value));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}