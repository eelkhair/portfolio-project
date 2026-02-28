using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class ListPublicCompaniesQuery : BaseQuery<List<PublicCompanyDto>>, IAnonymousRequest;

public class ListPublicCompaniesQueryHandler(IJobBoardQueryDbContext context, ILogger<ListPublicCompaniesQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<ListPublicCompaniesQuery, List<PublicCompanyDto>>
{
    public async Task<List<PublicCompanyDto>> HandleAsync(ListPublicCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await Context.Companies
            .Select(c => new PublicCompanyDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Website = c.Website,
                About = c.About,
                Founded = c.Founded,
                Size = c.Size,
                Logo = c.Logo,
                IndustryName = c.Industry.Name,
                JobCount = c.Jobs.Count
            })
            .ToListAsync(cancellationToken);
    }
}
