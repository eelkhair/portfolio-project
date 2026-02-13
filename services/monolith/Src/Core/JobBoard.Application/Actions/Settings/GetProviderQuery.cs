using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Settings;

public class GetProviderQuery : BaseQuery<ProviderSettings>
{
}

public class GetProviderQueryHandler(
    IJobBoardQueryDbContext context,
    ILogger<GetProviderQueryHandler> logger,
    IAiServiceClient aiServiceClient)
    : BaseQueryHandler(context, logger), IHandler<GetProviderQuery, ProviderSettings>
{
    public async Task<ProviderSettings> HandleAsync(GetProviderQuery request, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Fetching AI provider settings from AI Service v2...");

        var settings = await aiServiceClient.GetProvider(cancellationToken);

        Activity.Current?.SetTag("ai.provider", settings.Provider);
        Activity.Current?.SetTag("ai.model", settings.Model);
        Logger.LogInformation("Fetched AI provider settings: {Provider} / {Model}", settings.Provider, settings.Model);

        return settings;
    }
}
