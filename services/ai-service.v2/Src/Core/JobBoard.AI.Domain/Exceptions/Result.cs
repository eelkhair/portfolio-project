namespace JobBoard.AI.Domain.Exceptions;

public class Result<TValue>
{
    public TValue? Value { get; }
    public IReadOnlyCollection<Error> Errors { get; }
    public bool IsSuccess => Errors.Count == 0;
    public bool IsFailure => !IsSuccess;

    private Result(TValue value) { Value = value; Errors = []; }
    private Result(IReadOnlyCollection<Error> errors) { Errors = errors; }

    public static Result<TValue> Success(TValue value) => new(value);
    public static Result<TValue> Failure(IReadOnlyCollection<Error> errors) => new(errors);
}
