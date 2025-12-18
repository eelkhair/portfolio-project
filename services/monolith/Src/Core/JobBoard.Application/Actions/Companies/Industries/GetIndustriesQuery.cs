using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Industries;

public class GetIndustriesQuery: BaseQuery<IQueryable<IndustryDto>>;

public class GetIndustriesQueryHandler(IJobBoardQueryDbContext context, ILogger<GetIndustriesQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetIndustriesQuery, IQueryable<IndustryDto>>
{
    public Task<IQueryable<IndustryDto>> HandleAsync(GetIndustriesQuery request, CancellationToken cancellationToken)
    {
        var response = Context.Industries.Select(c=> new IndustryDto()
        {
            Id = c.Id,
            Name = c.Name,
            CreatedAt = c.CreatedAt,
            UpdatedAt= c.UpdatedAt,
            CreatedBy = c.CreatedBy,
            UpdatedBy = c.UpdatedBy
        });
        
        return Task.FromResult(response);
    }
}