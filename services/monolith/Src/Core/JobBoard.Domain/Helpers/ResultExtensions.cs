using JobBoard.Domain.Exceptions;

namespace JobBoard.Domain.Helpers;

public static class ResultExtensions
{
    public static TOut? Collect<T, TOut>(
        this Result<T> result,
        List<Error> errors)
        where T : class
    {
        if (!result.IsFailure) return (TOut?)(result.Value as dynamic)!.Value;
        errors.AddRange(result.Errors);
        return default;

    }
}