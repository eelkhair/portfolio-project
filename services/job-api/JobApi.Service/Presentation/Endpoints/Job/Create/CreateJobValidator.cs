using FastEndpoints;
using FluentValidation;
using JobAPI.Contracts.Job.Requests;

namespace JobApi.Presentation.Endpoints.Job.Create;

public class CreateJobValidator : Validator<CreateJobRequest>
{
    public CreateJobValidator()
    {
        RuleFor(c => c.Title).NotEmpty();
    }
}