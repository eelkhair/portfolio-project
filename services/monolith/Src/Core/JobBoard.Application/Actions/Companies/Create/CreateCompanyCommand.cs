using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using JobBoard.Application.Actions.Base;
using JobBoard.Application.Actions.Companies.Models;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Repositories;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Create;

public class CreateCompanyCommand : BaseCommand<CompanyDto>
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;

    [Url]
    public string? CompanyWebsite { get; set; }

    [Required]
    public Guid IndustryUId { get; set; }

    [Required]
    public string AdminFirstName { get; set; } = string.Empty;

    [Required]
    public string AdminLastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AdminEmail { get; set; } = string.Empty;
}

public class CreateCompanyCommandHandler(IHandlerContext context
    , ICompanyRepository companyRepository
    , IUserRepository userRepository) : BaseCommandHandler(context),
    IHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> HandleAsync(CreateCompanyCommand request, CancellationToken cancellationToken)
    {        
        request.SetActivityTagsForCompany(Activity.Current);
        Logger.LogInformation("Creating company {CompanyName}...", request.Name);
        var (companyId,companyUId) = await Context.GetNextValueFromSequenceAsync(typeof(Company), cancellationToken);
        var (userId, userUId) = await Context.GetNextValueFromSequenceAsync(typeof(User), cancellationToken);
        var (userCompanyId, userCompanyUId) = await Context.GetNextValueFromSequenceAsync(typeof(UserCompany), cancellationToken);
       
        var industryId = await companyRepository.GetIndustryIdByUId(request.IndustryUId, cancellationToken);

        var company = request.ToCompanyEntity(companyUId, companyId, industryId);
        var user = request.ToUserEntity(userUId, userId);
        var companyUser = request.ToUserCompanyEntity(userCompanyUId, userCompanyId, companyId, userId);
        
        await companyRepository.AddAsync(company, cancellationToken);
        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.AddCompanyUser(companyUser, cancellationToken);

        var integrationEvent = request.ToIntegrationEvent(companyUId, userUId, userCompanyUId);
        await OutboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await Context.SaveChangesAsync(request.UserId, cancellationToken);
        
        var parentActivity = Activity.Current;
        UnitOfWorkEvents.Enqueue(() =>
        {
            parentActivity?.SetTag("CompanyUId", companyUId.ToString());
            parentActivity?.SetTag("UserUId", userUId.ToString());
            parentActivity?.SetTag("UserCompanyUId", userCompanyUId.ToString());

            Logger.LogInformation(
                "Successfully created company {CompanyName} ({CompanyUId})",
                request.Name,
                companyUId);

            return Task.CompletedTask;
        });

        return new CompanyDto
        {
            Id = companyUId,
            Name = company.Name,
            Email= company.Email,
            Website = company.Website,
            Status = company.Status
        };
    }

    
}


