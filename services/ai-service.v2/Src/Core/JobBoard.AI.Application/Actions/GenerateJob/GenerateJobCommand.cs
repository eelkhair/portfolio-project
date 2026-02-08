using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;

namespace JobBoard.AI.Application.Actions.GenerateJob;

public class GenerateJobCommand(GenerateJobRequest request) : BaseCommand<GenerateJobResponse>
{
    public GenerateJobRequest Request { get; } = request;
}

public class GenerateJobCommandHandler(IHandlerContext context, 
    IAiPrompt<GenerateJobRequest> iaiPrompt,
    ICompletionService completionService
    ) : BaseCommandHandler(context),
    IHandler<GenerateJobCommand, GenerateJobResponse>
{
    public async Task<GenerateJobResponse> HandleAsync(GenerateJobCommand request, CancellationToken cancellationToken)
    {
        var userPrompt = iaiPrompt.BuildUserPrompt(request.Request);
        var systemPrompt = iaiPrompt.BuildSystemPrompt();
        
        var response = await completionService.GetResponseAsync<GenerateJobResponse>(systemPrompt, userPrompt, cancellationToken);
        
        return response;
    }
}