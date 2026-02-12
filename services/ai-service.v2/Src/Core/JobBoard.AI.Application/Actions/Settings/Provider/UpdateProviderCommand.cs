using System.Diagnostics;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Settings.Provider;

public class UpdateProviderCommand(UpdateProviderRequest request) : BaseCommand<Unit>
{
    public UpdateProviderRequest Request { get; } = request;
}

public class UpdateProviderCommandHandler(IHandlerContext context, ISettingsService settingsService) : BaseCommandHandler(context), IHandler<UpdateProviderCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateProviderCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("ai.provider", command.Request.Provider);
        Activity.Current?.SetTag("ai.model", command.Request.Model);

        await settingsService.UpdateProviderAsync(command.Request);
        return Unit.Value;
    }
}
