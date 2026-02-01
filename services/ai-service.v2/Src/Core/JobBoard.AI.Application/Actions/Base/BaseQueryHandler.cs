using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Base;

public abstract class BaseQueryHandler
{
    protected BaseQueryHandler(IHandlerContext handlerContext)
    {
        Logger = handlerContext.LoggerFactory.CreateLogger(GetType());
    }

    protected ILogger Logger { get; }
}
