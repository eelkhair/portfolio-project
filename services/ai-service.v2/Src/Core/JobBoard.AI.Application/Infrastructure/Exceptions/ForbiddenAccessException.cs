// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace JobBoard.AI.Application.Infrastructure.Exceptions;

// ReSharper disable once ClassNeverInstantiated.Global
public class ForbiddenAccessException : Exception
{ 
    public string? ResourceName { get; }
    public object? Key { get; }
    
    public ForbiddenAccessException(string resourceName, object key)
        : base($"Resource '{resourceName}' with key '{key}' was not found.")
    {
        ResourceName = resourceName;
        Key = key;
    }
    public ForbiddenAccessException() { }
    public ForbiddenAccessException(string? message) : base(message) { }
    public ForbiddenAccessException(string? message, Exception? innerException) : base(message, innerException) { }
}
