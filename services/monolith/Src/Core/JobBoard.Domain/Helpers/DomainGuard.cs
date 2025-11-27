using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Helpers;

public static class DomainGuard
{
    public static void AgainstInvalidId(
        long id, string errorCode, List<Error> errors)
    {
        if (id <= 0)
            errors.Add(new Error(errorCode, $"{errorCode} must be greater than zero."));
    }
    public static void AgainstInvalidId(
        Guid id, string errorCode, List<Error> errors)
    {
        if (Guid.Empty == id)
            errors.Add(new Error(errorCode, $"{errorCode} must be greater than zero."));
    }

    public static void AgainstNullOrEmpty(
        string? value, string errorCode, List<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(new Error(errorCode, $"{errorCode} cannot be empty."));
    }
}