using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Settings.ApplicationMode;

public class GetApplicationModeQuery : BaseQuery<ApplicationModeDto>;

public class GetApplicationModeQueryHandler(
    IJobBoardQueryDbContext context,
    ILogger<GetApplicationModeQueryHandler> logger,
    IAiServiceClient aiServiceClient)
    : BaseQueryHandler(context, logger), IHandler<GetApplicationModeQuery, ApplicationModeDto>
{
    public async Task<ApplicationModeDto> HandleAsync(GetApplicationModeQuery request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Fetching application Mode from AI Service v2...");

        var settings = await aiServiceClient.GetApplicationMode(cancellationToken);
        
        Activity.Current?.SetTag("IsMonolith", settings.IsMonolith);
        Logger.LogInformation("Fetched Application Mode: {IsMonolith} ", settings.IsMonolith);

        return settings;
    }
}
