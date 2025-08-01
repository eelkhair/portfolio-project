using FastEndpoints;
using FluentValidation;
using JobApi.Application.Interfaces;
using JobAPI.Contracts.Models.Jobs.Requests;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Presentation.Endpoints.Jobs.Create;

public class CreateJobValidator : Validator<CreateJobRequest>
{


    public CreateJobValidator()
    {


        RuleFor(c => c.Title).NotEmpty();

        RuleFor(c => c.CompanyUId)
            .MustAsync(CompanyExists)
            .WithMessage("Company does not exist");
    }

    private async Task<bool> CompanyExists(Guid uid, CancellationToken ct)
    { 
        return await Resolve<IJobDbContext>().Companies.AnyAsync(c => c.UId == uid, ct);
    }
}