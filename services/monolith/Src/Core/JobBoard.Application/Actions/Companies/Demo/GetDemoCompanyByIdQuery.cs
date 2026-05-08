using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Demo;

public class GetDemoCompanyByIdQuery : BaseQuery<CompanyDto?>
{
    public Guid CompanyUId { get; set; }
}

public class GetDemoCompanyByIdQueryHandler(IJobBoardQueryDbContext context, ILogger<GetDemoCompanyByIdQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetDemoCompanyByIdQuery, CompanyDto?>
{
    public async Task<CompanyDto?> HandleAsync(GetDemoCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        return await Context.Companies
            .Where(x => x.Id == request.CompanyUId && x.IsDemo)
            .Select(x => new CompanyDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                About = x.About,
                EEO = x.EEO,
                Email = x.Email,
                Founded = x.Founded,
                Logo = x.Logo,
                Website = x.Website,
                Phone = x.Phone,
                Size = x.Size,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                IndustryUId = x.Industry.Id
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
