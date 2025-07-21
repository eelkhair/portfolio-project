using System;
using System.Threading;
using System.Threading.Tasks;
using Elkhair.Dev.Common.Application.Abstractions.Dispatcher;

namespace JobApi.Application.Commands.Job;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Guid>
{

    public Task<Guid> HandleAsync(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid(); // Simulate DB call
        return Task.FromResult(id);
   
    }
}