using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.AI.Infrastructure.AI.AITools.Admins.System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Infrastructure.AI.AITools.Admins;

public class SystemAdminToolRegistry(
    IActivityFactory activityFactory,
    ISettingsService settingsService,
    ILogger<SystemAdminToolRegistry> logger
) : IAiTools
{
    public IEnumerable<AITool> GetTools()
    {
        yield return SetModeTool.Get(activityFactory, settingsService, logger);
    }
}
