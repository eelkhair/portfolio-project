using JobBoard.IntegrationEvents.Company;

namespace JobBoard.Application.Actions.Companies.Update;

public static class UpdateCompanyMappers
{
    public static CompanyUpdatedV1Event ToIntegrationEvent(this UpdateCompanyCommand command, Guid companyUId)
        => new(companyUId, command.IndustryUId) { UserId = command.UserId };
}
