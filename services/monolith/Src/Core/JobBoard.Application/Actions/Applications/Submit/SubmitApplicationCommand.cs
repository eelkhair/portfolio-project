using JobBoard.Application.Actions.Base;
using JobBoard.Monolith.Contracts.Public;

namespace JobBoard.Application.Actions.Applications.Submit;

public class SubmitApplicationCommand(SubmitApplicationRequest request) : BaseCommand<ApplicationResponse>
{
    public SubmitApplicationRequest Request { get; set; } = request;
}
