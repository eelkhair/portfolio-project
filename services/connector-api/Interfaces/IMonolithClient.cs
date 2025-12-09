using ConnectorAPI.Models;

namespace ConnectorAPI.Interfaces;

public interface IMonolithClient
{
    Task<(CompanyCreateCompanyResult Company, CompanyCreateUserResult Admin)>
        GetCompanyAndAdminForCreatedEventAsync(
            Guid companyId,
            Guid adminId,
            string userId,
            CancellationToken cancellationToken);
}