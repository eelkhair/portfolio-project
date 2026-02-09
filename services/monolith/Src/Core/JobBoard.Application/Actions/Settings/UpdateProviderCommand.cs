using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Settings.Models;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Settings;

public class UpdateProviderCommand : BaseCommand<bool>, INoTransaction
{
    public UpdateProviderRequest Request { get; set; } = new();
}

public class UpdateProviderCommandHandler(
    IHandlerContext handlerContext,
    IAiServiceClient aiServiceClient)
    : BaseCommandHandler(handlerContext), IHandler<UpdateProviderCommand, bool>
{
    public async Task<bool> HandleAsync(UpdateProviderCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("ai.provider", command.Request.Provider);
        Activity.Current?.SetTag("ai.model", command.Request.Model);
        Logger.LogInformation("Updating AI provider to {Provider} with model {Model}...",
            command.Request.Provider, command.Request.Model);

        await aiServiceClient.UpdateProvider(command.Request, cancellationToken);

        Logger.LogInformation("AI provider settings updated successfully");
        return true;
    }
}
