using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Settings.ApplicationMode;

public class UpdateApplicationModeCommand(ApplicationModeDto request): BaseCommand<ApplicationModeDto>
{
    public ApplicationModeDto Request { get; } = request;
}

public class UpdateApplicationModeCommandHandler(IHandlerContext handlerContext, ISettingsService settingsService) : 
    BaseCommandHandler(handlerContext), IHandler<UpdateApplicationModeCommand, ApplicationModeDto>
{
    public async Task<ApplicationModeDto> HandleAsync(UpdateApplicationModeCommand request, CancellationToken cancellationToken)
    {
        await settingsService.UpdateApplicationModeAsync(request.Request);
        return request.Request;
    }
}