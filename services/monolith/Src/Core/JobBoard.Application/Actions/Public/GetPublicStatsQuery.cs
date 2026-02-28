using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetPublicStatsQuery : BaseQuery<PublicStatsDto>, IAnonymousRequest;

public class GetPublicStatsQueryHandler(IJobBoardQueryDbContext context, ILogger<GetPublicStatsQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetPublicStatsQuery, PublicStatsDto>
{
    public Task<PublicStatsDto> HandleAsync(GetPublicStatsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
