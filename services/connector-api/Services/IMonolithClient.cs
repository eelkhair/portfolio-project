using ConnectorAPI.Models;

namespace ConnectorAPI.Services;

public interface IMonolithClient
{
    Task<(CompanyCreateCompanyResult Company, CompanyCreateUserResult Admin)>
        GetCompanyAndAdminForCreatedEventAsync(
            Guid companyId,
            Guid adminId,
            string userId,
            CancellationToken cancellationToken);
}