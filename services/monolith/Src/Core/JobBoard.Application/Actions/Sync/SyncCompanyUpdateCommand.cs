using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Sync;

public class SyncCompanyUpdateCommand : BaseCommand<Unit>
{
    public Guid CompanyId { get; set; }
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

/// <summary>
/// Reverse-sync handler: updates a Company from a microservice event.
/// Does NOT call OutboxPublisher to prevent infinite sync loops.
/// </summary>
public class SyncCompanyUpdateCommandHandler(
    IHandlerContext handlerContext,
    ICompanyRepository companyRepository)
    : BaseCommandHandler(handlerContext), IHandler<SyncCompanyUpdateCommand, Unit>
{
    public async Task<Unit> HandleAsync(SyncCompanyUpdateCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("sync.company.id", command.CompanyId);
        Activity.Current?.SetTag("sync.company.name", command.Name);
        Logger.LogInformation("Reverse-sync: updating company {CompanyId}", command.CompanyId);

        var company = await companyRepository.GetCompanyById(command.CompanyId, cancellationToken);

        company.SetName(command.Name);
        company.SetEmail(command.CompanyEmail);
        company.SetWebsite(command.CompanyWebsite);
        company.SetPhone(command.Phone);
        company.SetDescription(command.Description);
        company.SetAbout(command.About);
        company.SetEEO(command.EEO);
        company.SetFounded(command.Founded);
        company.SetSize(command.Size);
        company.SetLogo(command.Logo);

        var industryId = await companyRepository.GetIndustryIdByUId(command.IndustryUId, cancellationToken);
        company.SetIndustry(industryId);

        // No OutboxPublisher.PublishAsync() — prevents reverse-sync → forward-sync loop
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Logger.LogInformation("Reverse-sync: updated company {CompanyId}", command.CompanyId);

        return Unit.Value;
    }
}
