using System.Diagnostics;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Entities.Users;
using JobBoard.IntegrationEvents.Company;

namespace JobBoard.Application.Actions.Companies.Create;

public static class CreateCompanyMappers
{
    public static Company ToCompanyEntity(this CreateCompanyCommand command, Guid uid, int id, int industryId)
    {
        var company = Company.Create(new CompanyInput(
            Name: command.Name,
            Email: command.CompanyEmail,
            Status: "Provisioning",
            InternalId: id, Id: uid,
            IndustryId: industryId));
        
        company.SetWebsite(command.CompanyWebsite);
        return company;
    }

    public static User ToUserEntity(this CreateCompanyCommand command, Guid uid, int id)
    => User.Create(command.AdminFirstName, command.AdminLastName, command.AdminEmail, null, uid, id);
    
    public static UserCompany ToUserCompanyEntity(this CreateCompanyCommand command,Guid uid, int id, int companyId, int userId)
        => UserCompany.Create(userId, companyId, id, uid);

    public static CompanyCreatedV1Event ToIntegrationEvent(this CreateCompanyCommand command, Guid companyUId,
        Guid adminUId, Guid userCompanyUId)
        => new (
            companyUId,
            command.IndustryUId,
            adminUId, userCompanyUId
   ){UserId = command.UserId};
    
    public static void SetActivityTagsForCompany(this CreateCompanyCommand request, Activity? activity)
    {
        activity?.SetTag("CompanyName", request.Name);
        activity?.SetTag("CompanyEmail", request.CompanyEmail);
        activity?.SetTag("AdminEmail", request.AdminEmail);   
        activity?.SetTag("IndustryUId", request.IndustryUId.ToString());
    }
}