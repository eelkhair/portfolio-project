using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Settings.ApplicationMode;

public class UpdateApplicationModeCommand(ApplicationModeDto request) : BaseCommand<ApplicationModeDto>
{
    public ApplicationModeDto Request { get; } = request;
}

public class UpdateApplicationModeCommandHandler(IHandlerContext handlerContext, IAiServiceClient aiServiceClient) : 
    BaseCommandHandler(handlerContext), IHandler<UpdateApplicationModeCommand, ApplicationModeDto>
{
    public async Task<ApplicationModeDto> HandleAsync(UpdateApplicationModeCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("IsMonolith", command.Request.IsMonolith);
        Logger.LogInformation("Updating application mode to {IsMonolith}...",
            command.Request.IsMonolith);

        await aiServiceClient.UpdateApplicationMode(command.Request, cancellationToken);

        Logger.LogInformation("Application Mode updated successfully");
       
        return command.Request;
    }
}