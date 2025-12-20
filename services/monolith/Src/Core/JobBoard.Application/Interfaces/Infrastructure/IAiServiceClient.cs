using JobBoard.Application.Actions.Jobs.Models;

namespace JobBoard.Application.Interfaces.Infrastructure;

public interface IAiServiceClient
{
    Task<List<JobDraftResponse>> ListDrafts(Guid companyId, CancellationToken cancellationToken);
}