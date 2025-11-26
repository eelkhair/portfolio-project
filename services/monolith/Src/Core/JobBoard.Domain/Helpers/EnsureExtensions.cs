

using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Helpers;

public static class EnsureExtensions
{
    public static TOut? Ensure<T, TOut>(this Result<T> result, string errorCode)
        where T : class
    {
        if (result.IsFailure)
            throw new DomainException(errorCode, result.Errors);

        return (TOut?)(result.Value as dynamic)!.Value;
    }
}
