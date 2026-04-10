using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.Settings.FeatureFlags;

public class UpdateFeatureFlagCommand(UpdateFeatureFlagRequest request) : BaseCommand<UpdateFeatureFlagRequest>
{
    public UpdateFeatureFlagRequest Request { get; } = request;
}

public class UpdateFeatureFlagCommandHandler(IHandlerContext handlerContext, ISettingsService settingsService)
    : BaseCommandHandler(handlerContext), IHandler<UpdateFeatureFlagCommand, UpdateFeatureFlagRequest>
{
    public async Task<UpdateFeatureFlagRequest> HandleAsync(UpdateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        await settingsService.UpdateFeatureFlagAsync(request.Request);
        return request.Request;
    }
}
