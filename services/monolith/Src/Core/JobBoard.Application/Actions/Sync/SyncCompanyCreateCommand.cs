using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Sync;

public class SyncCompanyCreateCommand : BaseCommand<Unit>
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public Guid IndustryUId { get; set; }
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public Guid? AdminUId { get; set; }
    public Guid? UserCompanyUId { get; set; }
}

/// <summary>
/// Reverse-sync handler: creates a Company + User + UserCompany from a microservice event.
/// Full provisioning so user data is available for reports.
/// Does NOT call OutboxPublisher to prevent infinite sync loops.
/// </summary>
public class SyncCompanyCreateCommandHandler(
    IHandlerContext handlerContext,
    ICompanyRepository companyRepository,
    IUserRepository userRepository)
    : BaseCommandHandler(handlerContext), IHandler<SyncCompanyCreateCommand, Unit>
{
    public async Task<Unit> HandleAsync(SyncCompanyCreateCommand command, CancellationToken cancellationToken)
    {
        Activity.Current?.SetTag("sync.company.id", command.CompanyId);
        Activity.Current?.SetTag("sync.company.name", command.Name);
        Logger.LogInformation("Reverse-sync: creating company {CompanyName} ({CompanyId})",
            command.Name, command.CompanyId);

        // Idempotency: skip if company already exists
        var queryContext = (IJobBoardQueryDbContext)Context;
        var exists = await queryContext.Companies
            .AnyAsync(c => c.Id == command.CompanyId, cancellationToken);

        if (exists)
        {
            Logger.LogInformation("Reverse-sync: company {CompanyId} already exists, skipping", command.CompanyId);
            Activity.Current?.SetTag("sync.company.skipped", true);
            return Unit.Value;
        }

        // Generate sequence IDs
        var (companyInternalId, companyUId) = await Context.GetNextValueFromSequenceAsync(typeof(Company), cancellationToken);
        var (userId, userUId) = await Context.GetNextValueFromSequenceAsync(typeof(User), cancellationToken);
        var (userCompanyId, userCompanyUId) = await Context.GetNextValueFromSequenceAsync(typeof(UserCompany), cancellationToken);

        // Use the microservice's UIds if provided, otherwise use generated ones
        var finalCompanyUId = command.CompanyId;
        var finalAdminUId = command.AdminUId ?? userUId;
        var finalUserCompanyUId = command.UserCompanyUId ?? userCompanyUId;

        // Resolve industry
        var industryId = await companyRepository.GetIndustryIdByUId(command.IndustryUId, cancellationToken);

        // Create Company entity with the microservice's CompanyUId
        var company = Company.Create(new CompanyInput(
            Name: command.Name,
            Email: command.CompanyEmail,
            Status: "Provisioning",
            InternalId: companyInternalId,
            Id: finalCompanyUId,
            IndustryId: industryId));
        company.SetWebsite(command.CompanyWebsite);

        // Create User entity
        var user = User.Create(
            command.AdminFirstName, command.AdminLastName, command.AdminEmail,
            null, finalAdminUId, userId);

        // Create UserCompany link
        var userCompany = UserCompany.Create(userId, companyInternalId, userCompanyId, finalUserCompanyUId);

        await companyRepository.AddAsync(company, cancellationToken);
        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.AddCompanyUser(userCompany, cancellationToken);

        // No OutboxPublisher.PublishAsync() — prevents reverse-sync → forward-sync loop
        await Context.SaveChangesAsync(command.UserId, cancellationToken);

        Activity.Current?.SetTag("sync.company.created", true);
        Activity.Current?.SetTag("sync.company.adminUId", finalAdminUId);
        Logger.LogInformation(
            "Reverse-sync: created company {CompanyName} ({CompanyUId}) with admin user ({AdminUId})",
            command.Name, finalCompanyUId, finalAdminUId);

        return Unit.Value;
    }
}
