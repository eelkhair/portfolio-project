using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Get;

public class GetCompanyByUIdQuery: BaseQuery<IQueryable<CompanyDto>>
{
    public Guid UId { get; set; }
}

public class GetCompanyByUIdQueryHandler(IJobBoardDbContext context, ILogger<GetCompanyByUIdQueryHandler> logger)
    : BaseQueryHandler(context, logger), IHandler<GetCompanyByUIdQuery, IQueryable<CompanyDto>>
{
    public Task<IQueryable<CompanyDto>> HandleAsync(GetCompanyByUIdQuery request, CancellationToken cancellationToken)
    {
        var result = Context.Companies.Where(x=>x.UId == request.UId).Select(x=> new CompanyDto
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
                UId = x.UId,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy,
                Industry = new IndustryDto
                {
                    UId = x.Industry.UId,
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