using System.Text.RegularExpressions;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Company;

public partial class CompanyEmail
{
    public string Value { get; }
    private static readonly Regex FormatRegex = MyRegex();

    private CompanyEmail(string value) { Value = value; }

    public static Result<CompanyEmail> Create(string value)
    {
        if (string.IsNullOrEmpty(value)) value = string.Empty;
        value = value.Trim();
        
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new Error("Email.Empty", "Email cannot be empty."));
        }
        if (value.Length > 100)
        {
            errors.Add(new Error("Email.TooLong", "Email cannot exceed 100 characters."));
        }
        if (!string.IsNullOrWhiteSpace(value) && !FormatRegex.IsMatch(value))
        {
            errors.Add(new Error("Email.InvalidFormat", "Email is not in a valid format."));
        }
        return errors.Count > 0 ? Result<CompanyEmail>.Failure(errors) : 
            Result<CompanyEmail>.Success(new CompanyEmail(value));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}