using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyApi.Infrastructure.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Features.Companies.Create;

public class CreateCompanyValidator: Validator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
       
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters")
            .MustAsync(async (name, ct) =>
            {
                var db = Resolve<ICompanyDbContext>();
                return !await db.Companies.AnyAsync(c => c.Name == name, ct);
            }).WithMessage("Company name already exists");
        
        RuleFor(c => c.About).MaximumLength(2000);
        RuleFor(c => c.EEO).MaximumLength(500);
    }
}