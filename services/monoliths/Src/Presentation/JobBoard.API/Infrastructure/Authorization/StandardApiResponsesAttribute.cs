// ReSharper disable InconsistentNaming
namespace JobBoard.API.Infrastructure.Authorization;

/// <summary>
/// Attribute to indicate that a method has standard API responses.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class StandardApiResponsesAttribute : Attribute
{
    /// <summary>
    /// Gets a value indicating whether to include the 400 Bad Request response documentation.
    /// </summary>
    public bool Include400BadRequest { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardApiResponsesAttribute"/> class.
    /// </summary>
    /// <param name="Include400BadRequest">
    /// Set to true to include the 400 Bad Request response. Defaults to true.
    /// This is typically needed for endpoints that accept a body or complex query parameters.
    /// </param>
    public StandardApiResponsesAttribute(bool Include400BadRequest = true)
    {
        this.Include400BadRequest = Include400BadRequest;
    }
}