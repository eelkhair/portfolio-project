using FastEndpoints;
using FluentValidation;
using JobAPI.Contracts.Models.Companies.Requests;
using JobApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace JobApi.Presentation.Endpoints.Companies.Create;

public class CreateCompanyValidator: Validator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
       
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters")
            .MustAsync(async (name, ct) =>
            {
                var db = Resolve<IJobDbContext>();
                return !await db.Companies.AnyAsync(c => c.Name == name, ct);
            }).WithMessage("Company name already exists");
        
        RuleFor(c => c.About).MaximumLength(2000);
        RuleFor(c => c.EEO).MaximumLength(500);
    }
}