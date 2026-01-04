using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Activate;

public class ActivateCompanyCommand(CompanyCreatedModel model) : BaseCommand<Unit>
{
    public CompanyCreatedModel Model { get; set; } = model;
}

public class ActivateCompanyCommandHandler(IHandlerContext handlerContext,
    IActivityFactory activityFactory,
    ICompanyRepository companyRepository,
    IUserRepository userRepository)
    : BaseCommandHandler(handlerContext), IHandler<ActivateCompanyCommand, Unit>
{
    public async Task<Unit> HandleAsync(ActivateCompanyCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity("ActivateCompany", ActivityKind.Internal);
        
        activity?.SetTag("activated.company.name",request.Model.CompanyName);
        activity?.SetTag("activated.company.id",request.Model.CompanyUId);
        
        Logger.LogInformation("Activating company; Name: {CompanyName}; Id: {CompanyUId}", request.Model.CompanyName, request.Model.CompanyUId);
        var company = await companyRepository.GetCompanyById(request.Model.CompanyUId, cancellationToken);
        company.SetStatus("Active");
        company.SetExternalId(request.Model.Auth0CompanyId);
        
        var user = await userRepository.FindUserByUIdAsync(request.Model.UserUId, cancellationToken);
        user.SetExternalId(request.Model.Auth0UserId);
        
        await Context.SaveChangesAsync(request.UserId, cancellationToken);
        
        UnitOfWorkEvents.Enqueue(() =>
        {
            Logger.LogInformation("Successfully activated company; Name: {CompanyName}; Id: {CompanyUId}", request.Model.CompanyName, request.Model.CompanyUId);
            Activity.Current?.SetTag("status", "completed");
            return Task.CompletedTask;
        });
        return Unit.Value;
    }
}