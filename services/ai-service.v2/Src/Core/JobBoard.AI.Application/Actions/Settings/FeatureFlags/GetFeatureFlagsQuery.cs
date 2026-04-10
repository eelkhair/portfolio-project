using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Settings.FeatureFlags;

public class GetFeatureFlagsQuery : BaseQuery<List<UpdateFeatureFlagRequest>>;

public class GetFeatureFlagsQueryHandler(ILogger<GetFeatureFlagsQuery> logger, ISettingsService settingsService)
    : BaseQueryHandler(logger), IHandler<GetFeatureFlagsQuery, List<UpdateFeatureFlagRequest>>
{
    public async Task<List<UpdateFeatureFlagRequest>> HandleAsync(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        return await settingsService.GetFeatureFlagsAsync();
    }
}
