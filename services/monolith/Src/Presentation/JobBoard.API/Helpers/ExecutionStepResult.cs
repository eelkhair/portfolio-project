// In: JobBoard.API.Controllers.ExecutionStepResult.cs (or a similar location)

using Microsoft.AspNetCore.Mvc;

namespace JobBoard.API.Helpers;

/// <summary>
/// An internal result object used by the BaseApiController for safely chaining operations.
/// It is NOT returned to the client. It holds either the successful data or the failed IActionResult.
/// </summary>
public class ExecutionStepResult<T>
{
    /// <summary>
    /// Indicates whether the execution step was successful.
    /// </summary>
    public bool IsSuccess { get; init; }
    /// <summary>
    /// Holds the data if the execution was successful; otherwise, null.
    /// </summary>
    public T? Data { get; init; }
    /// <summary>
    /// Holds the IActionResult if the execution failed; otherwise, null.
    /// </summary>
    public IActionResult? ErrorResult { get; init; }
}