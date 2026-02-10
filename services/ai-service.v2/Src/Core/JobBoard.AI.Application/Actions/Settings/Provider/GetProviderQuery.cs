using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Settings.Provider;

public class GetProviderQuery : BaseQuery<GetProviderResponse>
{
    
}

public class GetProviderQueryHandler(ILogger<GetProviderQuery> logger, ISettingsService settingsService) : BaseQueryHandler(logger), IHandler<GetProviderQuery, GetProviderResponse>
{
    public async Task<GetProviderResponse> HandleAsync(GetProviderQuery request, CancellationToken cancellationToken)
    {
        return await settingsService.GetProviderAsync();
    }
}