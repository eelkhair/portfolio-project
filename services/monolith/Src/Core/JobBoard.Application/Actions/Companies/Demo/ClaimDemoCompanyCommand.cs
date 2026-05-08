using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Demo;

public class ClaimDemoCompanyCommand : BaseCommand<Unit>, IAnonymousRequest
{
    public Guid CompanyUId { get; set; }
    public string ClaimToken { get; set; } = string.Empty;
    public string NewAdminEmail { get; set; } = string.Empty;
    public string NewAdminFirstName { get; set; } = string.Empty;
    public string NewAdminLastName { get; set; } = string.Empty;
    public string NewKeycloakUserId { get; set; } = string.Empty;
}

public class ClaimDemoCompanyCommandHandler(
    IHandlerContext context,
    IJobBoardQueryDbContext queryContext,
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    IDemoClaimTokenService claimTokenService) : BaseCommandHandler(context),
    IHandler<ClaimDemoCompanyCommand, Unit>
{
    public async Task<Unit> HandleAsync(ClaimDemoCompanyCommand request, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        activity?.SetTag("company.uid", request.CompanyUId.ToString());
        activity?.SetTag("ai.operation", "claim_demo_company");

        if (!claimTokenService.Verify(request.ClaimToken, request.CompanyUId))
        {
            Logger.LogWarning("Invalid claim token for company {CompanyUId}", request.CompanyUId);
            throw new UnauthorizedAccessException("Invalid or expired claim token");
        }

        var company = await companyRepository.GetCompanyById(request.CompanyUId, cancellationToken);

        if (!company.IsDemo)
        {
            Logger.LogWarning("Company {CompanyUId} is not a demo company or already claimed", request.CompanyUId);
            throw new InvalidOperationException("Company is not a demo company or has already been claimed");
        }

        var syntheticAdmin = await queryContext.UserCompanies
            .Where(uc => uc.CompanyId == company.InternalId)
            .Select(uc => uc.User)
            .FirstOrDefaultAsync(cancellationToken)
                              ?? throw new InvalidOperationException("Synthetic admin user not found for demo company");

        var (newUserId, newUserUId) = await Context.GetNextValueFromSequenceAsync(typeof(User), cancellationToken);
        var (userCompanyId, userCompanyUId) = await Context.GetNextValueFromSequenceAsync(typeof(UserCompany), cancellationToken);

        var newUser = User.Create(
            request.NewAdminFirstName,
            request.NewAdminLastName,
            request.NewAdminEmail,
            request.NewKeycloakUserId,
            newUserUId,
            newUserId);

        var newUserCompany = UserCompany.Create(newUserId, company.InternalId, userCompanyId, userCompanyUId);

        await userRepository.AddAsync(newUser, cancellationToken);
        await userRepository.AddCompanyUser(newUserCompany, cancellationToken);

        company.ClearDemoFlag();

        var claimedEvent = new DemoCompanyClaimedV1Event(
            company.Id,
            newUserUId,
            request.NewAdminEmail,
            request.NewAdminFirstName,
            request.NewAdminLastName,
            syntheticAdmin.Id)
        {
            UserId = request.UserId
        };

        await OutboxPublisher.PublishAsync(claimedEvent, cancellationToken);
        await Context.SaveChangesAsync(request.UserId, cancellationToken);

        UnitOfWorkEvents.Enqueue(() =>
        {
            Logger.LogInformation(
                "Demo company {CompanyUId} claimed by {NewAdminEmail}",
                company.Id,
                request.NewAdminEmail);
            return Task.CompletedTask;
        });

        return Unit.Value;
    }
}
