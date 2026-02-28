using JobBoard.Application.Actions.Base;
using JobBoard.Application.Infrastructure.Exceptions;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Public;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Public;

public class GetPublicCompanyByIdQuery(Guid id) : BaseQuery<PublicCompanyDto>, IAnonymousRequest
{
    public Guid Id { get; } = id;
}

public class GetPublicCompanyByIdQueryHandler(IJobBoardQueryDbContext context, ILogger<GetPublicCompanyByIdQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetPublicCompanyByIdQuery, PublicCompanyDto>
{
    public async Task<PublicCompanyDto> HandleAsync(GetPublicCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await Context.Companies
            .Where(c => c.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        return company ?? throw new NotFoundException("Company", request.Id);
    }
}
