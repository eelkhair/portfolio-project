using FastEndpoints;
using FluentValidation;
using JobAPI.Contracts.Jobs.Requests;

namespace JobApi.Presentation.Endpoints.Jobs.Create;

public class CreateJobValidator : Validator<CreateJobRequest>
{
    public CreateJobValidator()
    {
        RuleFor(c => c.Title).NotEmpty();
    }
}