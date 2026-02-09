using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Settings;

public class UpdateProviderCommand(UpdateProviderRequest request) : BaseCommand<Unit>
{
    public UpdateProviderRequest Request { get; } = request;
}

public class UpdateProviderCommandHandler(IHandlerContext context, ISettingsService settingsService) : BaseCommandHandler(context), IHandler<UpdateProviderCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        await settingsService.UpdateProviderAsync(request.Request);
        return Unit.Value;
    }
}
