namespace JobBoard.AI.Domain.Exceptions;

public class DomainException(string errorCode, IReadOnlyCollection<Error> errors)
    : Exception($"Domain validation failed with code: {errorCode}. See Errors for details.")
{
    public IReadOnlyCollection<Error> Errors { get; } = errors;
}
