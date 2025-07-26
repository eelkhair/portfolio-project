using System.Security.Claims;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;
using JobApi.Application.Interfaces;
using JobApi.Data.Entities;

namespace JobApi.Application.Commands.Companies;

public class CreateCompanyCommand(ClaimsPrincipal user, ILogger logger, Company company): BaseCommand<Company>(user, logger), ICommand<Company>
{
    public Company Company { get; } = company;
}

public class CreateCompanyCommandHandler(IJobDbContext context) : BaseCommandHandler(context), IRequestHandler<CreateCompanyCommand, Company>
{
    public async Task<Company> HandleAsync(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        Context.Companies.Add(request.Company);
        await Context.SaveChangesAsync(request.User, cancellationToken);
        return request.Company;
    }
}