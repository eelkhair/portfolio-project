using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents.Company;
using JobBoard.Monolith.Contracts.Companies;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Demo;

/// <summary>
/// Anonymous version of <c>CreateCompanyCommand</c> for the landing-page chatbot.
/// Marked <c>IAnonymousRequest</c> so the user-context decorator skips auth checks.
/// Forces <c>IsDemo = true</c> + <c>DemoExpiresAt = UtcNow + 1h</c>.
/// </summary>
public class CreateDemoCompanyCommand : BaseCommand<CompanyDto>, IAnonymousRequest
{
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public Guid IndustryUId { get; set; }
    public DateTime DemoExpiresAt { get; set; }
}

public class CreateDemoCompanyCommandHandler(
    IHandlerContext context,
    ICompanyRepository companyRepository,
    IUserRepository userRepository) : BaseCommandHandler(context),
    IHandler<CreateDemoCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> HandleAsync(CreateDemoCompanyCommand request, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        activity?.SetTag("CompanyName", request.Name);
        activity?.SetTag("CompanyEmail", request.CompanyEmail);
        activity?.SetTag("AdminEmail", request.AdminEmail);
        activity?.SetTag("IndustryUId", request.IndustryUId.ToString());
        activity?.SetTag("company.is_demo", true);
        activity?.SetTag("company.demo_expires_at", request.DemoExpiresAt.ToString("O"));

        Logger.LogInformation("Creating demo company {CompanyName}", request.Name);

        var (companyId, companyUId) = await Context.GetNextValueFromSequenceAsync(typeof(Company), cancellationToken);
        var (userId, userUId) = await Context.GetNextValueFromSequenceAsync(typeof(User), cancellationToken);
        var (userCompanyId, userCompanyUId) =
            await Context.GetNextValueFromSequenceAsync(typeof(UserCompany), cancellationToken);

        var industryId = await companyRepository.GetIndustryIdByUId(request.IndustryUId, cancellationToken);

        var company = Company.Create(new Domain.Aggregates.CompanyInput(
            Name: request.Name,
            Email: request.CompanyEmail,
            Status: "Provisioning",
            InternalId: companyId,
            Id: companyUId,
            IndustryId: industryId,
            IsDemo: true,
            DemoExpiresAt: request.DemoExpiresAt));

        var user = User.Create(
            request.AdminFirstName,
            request.AdminLastName,
            request.AdminEmail,
            externalId: null,
            id: userUId,
            internalId: userId);

        var companyUser = UserCompany.Create(userId, companyId, userCompanyId, userCompanyUId);

        await companyRepository.AddAsync(company, cancellationToken);
        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.AddCompanyUser(companyUser, cancellationToken);

        var integrationEvent = new CompanyCreatedV1Event(
            companyUId,
            request.IndustryUId,
            userUId,
            userCompanyUId)
        {
            UserId = "demo-anonymous",
            IsDemo = true,
            DemoExpiresAt = request.DemoExpiresAt
        };

        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync("demo-anonymous", cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            Logger.LogInformation(
                "Successfully created demo company {CompanyName} ({CompanyUId}); auto-deletes at {ExpiresAt:O}",
                request.Name, companyUId, request.DemoExpiresAt);
            return Task.CompletedTask;
        });

        return new CompanyDto
        {
            Id = companyUId,
            Name = company.Name,
            Email = company.Email,
            Status = company.Status
        };
    }
}
