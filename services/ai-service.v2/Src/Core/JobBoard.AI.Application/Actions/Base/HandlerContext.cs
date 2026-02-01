using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Base;

public class HandlerContext(ILoggerFactory loggerFactory) : IHandlerContext
{
    public ILoggerFactory LoggerFactory { get; } = loggerFactory;
}
