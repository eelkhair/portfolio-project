using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Interfaces.Configurations;

public interface IHandlerContext
{
    ILoggerFactory LoggerFactory { get; }
}
