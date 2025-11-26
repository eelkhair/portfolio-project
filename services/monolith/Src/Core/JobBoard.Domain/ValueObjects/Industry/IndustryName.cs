using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.ValueObjects.Industry;

public sealed class IndustryName
{
    public string Value { get; }

    private IndustryName(string value)
    {
        Value = value;
    }

    public static Result<IndustryName> Create(string name)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new Error("Name.Empty", "Industry name is required."));
            return Result<IndustryName>.Failure(errors);
        }

        name = name.Trim();

        if (name.Length > 250)
        {
            errors.Add(new Error("Name.TooLong",
                "Industry name cannot exceed 250 characters."));
        }

        return errors.Any()
            ? Result<IndustryName>.Failure(errors)
            : Result<IndustryName>.Success(new IndustryName(name));
    }
}