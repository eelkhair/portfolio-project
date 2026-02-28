using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Get;

public class GetCompaniesQuery : BaseQuery<IQueryable<CompanyDto>>;

public class GetCompaniesQueryHandler(IJobBoardDbContext context, ILogger<GetCompaniesQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetCompaniesQuery, IQueryable<CompanyDto>>
{
    public Task<IQueryable<CompanyDto>> HandleAsync(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var result = Context.Companies.Select(x=> new CompanyDto
        {
            Name= x.Name,
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
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            IndustryUId = x.Industry.Id,
            Industry = new IndustryDto
            {
                Id = x.Industry.Id,
                Name = x.Industry.Name,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                UpdatedBy = x.UpdatedBy,
                UpdatedAt = x.UpdatedAt,
            }
        });
        return Task.FromResult(result);
    }
}