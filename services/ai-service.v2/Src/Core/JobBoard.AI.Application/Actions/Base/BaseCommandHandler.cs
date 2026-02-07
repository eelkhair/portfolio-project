using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;


// ReSharper disable SuspiciousTypeConversion.Global

namespace JobBoard.AI.Application.Actions.Base;

public abstract class BaseCommandHandler
{
    protected BaseCommandHandler(IHandlerContext handlerContext)
    {
        Logger = handlerContext.LoggerFactory.CreateLogger(GetType());
    }

    protected ILogger Logger { get; }

}