using FastEndpoints;
using FluentValidation;
using JobAPI.Contracts.Models.Jobs.Requests;
using JobApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Features.Jobs.Create;

public class CreateJobValidator : Validator<CreateJobRequest>
{
    public CreateJobValidator()
    {
        RuleFor(c => c.Title).NotEmpty();
        RuleFor(c => c.AboutRole).NotEmpty();

        RuleFor(c => c.CompanyUId)
            .MustAsync(CompanyExists)
            .WithMessage("Company does not exist");
    }

    private async Task<bool> CompanyExists(Guid uid, CancellationToken ct)
    { 
        return await Resolve<IJobDbContext>().Companies.AnyAsync(c => c.UId == uid, ct);
    }
}