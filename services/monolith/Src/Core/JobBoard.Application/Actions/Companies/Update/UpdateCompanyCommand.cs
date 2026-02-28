using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Update;

public class UpdateCompanyCommand : BaseCommand<CompanyDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? About { get; set; }
    public string? EEO { get; set; }
    public DateTime? Founded { get; set; }
    public string? Size { get; set; }
    public string? Logo { get; set; }
    public Guid IndustryUId { get; set; }
}

public class UpdateCompanyCommandHandler(
    IHandlerContext context,
    ICompanyRepository companyRepository)
    : BaseCommandHandler(context), IHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> HandleAsync(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("CompanyUId", request.Id.ToString());
        Activity.Current?.SetTag("CompanyName", request.Name);
        Logger.LogInformation("Updating company {CompanyId}...", request.Id);

        var company = await companyRepository.GetCompanyById(request.Id, cancellationToken);

        company.SetName(request.Name);
        company.SetEmail(request.CompanyEmail);
        company.SetWebsite(request.CompanyWebsite);
        company.SetPhone(request.Phone);
        company.SetDescription(request.Description);
        company.SetAbout(request.About);
        company.SetEEO(request.EEO);
        company.SetFounded(request.Founded);
        company.SetSize(request.Size);
        company.SetLogo(request.Logo);

        var industryId = await companyRepository.GetIndustryIdByUId(request.IndustryUId, cancellationToken);
        company.SetIndustry(industryId);

        await Context.SaveChangesAsync(request.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            Logger.LogInformation("Successfully updated company {CompanyId}", request.Id);
            return Task.CompletedTask;
        });

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Email = company.Email,
            Website = company.Website,
            Description = company.Description,
            Phone = company.Phone,
            About = company.About,
            EEO = company.EEO,
            Founded = company.Founded,
            Size = company.Size,
            Logo = company.Logo,
            Status = company.Status,
            IndustryUId = request.IndustryUId
        };
    }
}
