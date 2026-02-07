using Microsoft.Extensions.Logging;

// ReSharper disable SuspiciousTypeConversion.Global

namespace JobBoard.AI.Application.Actions.Base;

public abstract class BaseQueryHandler(ILogger logger)
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    protected ILogger Logger { get; } = logger;
}
