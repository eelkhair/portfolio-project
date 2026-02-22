using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Settings.ApplicationMode;

public class GetApplicationModeQuery: BaseQuery<ApplicationModeDto>;
public class GetApplicationModeQueryHandler(ILogger<GetApplicationModeQuery> logger, ISettingsService settingsService) 
    : BaseQueryHandler(logger), IHandler<GetApplicationModeQuery, ApplicationModeDto>
{
    public async Task<ApplicationModeDto> HandleAsync(GetApplicationModeQuery request, CancellationToken cancellationToken)
    {
        return await settingsService.GetApplicationModeAsync();
    }
}