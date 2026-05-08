using JobBoard.Application.Actions.Base;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Demo;

public class DeleteDemoCompanyCommand : BaseCommand<Unit>, IAnonymousRequest
{
    public Guid CompanyUId { get; set; }
}

public class DeleteDemoCompanyCommandHandler(
    IHandlerContext context,
    IJobBoardQueryDbContext queryContext,
    ICompanyRepository companyRepository,
    IUserRepository userRepository) : BaseCommandHandler(context),
    IHandler<DeleteDemoCompanyCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteDemoCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetCompanyById(request.CompanyUId, cancellationToken);

        if (!company.IsDemo)
        {
            Logger.LogWarning(
                "Refusing to hard-delete non-demo company {CompanyUId} via demo cleanup endpoint",
                request.CompanyUId);
            throw new InvalidOperationException("Company is not a demo company");
        }

        // Pull dependent rows so EF can apply cascading delete via the change tracker.
        // The schema uses temporal tables — these deletes mirror to history automatically.
        var userCompanies = await queryContext.UserCompanies
            .Where(uc => uc.CompanyId == company.InternalId)
            .ToListAsync(cancellationToken);

        var userIds = userCompanies.Select(uc => uc.UserId).ToList();
        var users = await queryContext.Users
            .Where(u => userIds.Contains(u.InternalId))
            .ToListAsync(cancellationToken);

        await companyRepository.DeleteAsync(company, cancellationToken);
        foreach (var uc in userCompanies)
            await userRepository.DeleteCompanyUser(uc, cancellationToken);
        foreach (var user in users)
            await userRepository.DeleteAsync(user, cancellationToken);

        await Context.SaveChangesAsync(request.UserId, cancellationToken);

        Logger.LogInformation(
            "Hard-deleted demo company {CompanyUId} and {UserCount} synthetic admin user(s)",
            company.Id, users.Count);

        return Unit.Value;
    }
}
