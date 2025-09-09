using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyApi.Infrastructure.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public class CreateCompanyValidator : Validator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
        RuleFor(c => c.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters")
            .MustAsync(async (model, name, ct) =>
            {
                var db = Resolve<ICompanyDbContext>();
                return !await db.Companies.AnyAsync(x => x.Name == name, ct);
            })
            .WithMessage("Company name already exists");

        RuleFor(c => c.CompanyEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company email is required")
            .EmailAddress().WithMessage("Company email is not valid")
            .MaximumLength(100).WithMessage("Company email cannot exceed 100 characters")
            .MustAsync(async (model, email , ct) =>
            {
                var db = Resolve<ICompanyDbContext>();
                return !await db.Companies.AnyAsync(x => x.Email == email, ct);
            })
            .WithMessage("Company email already exists");

        RuleFor(c => c.CompanyWebsite)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(100).WithMessage("Company website cannot exceed 100 characters")
            .Must(url => string.IsNullOrWhiteSpace(url) ||
                         Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Company website is not a valid URL");

        RuleFor(c => c.IndustryUId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Industry is required")
            .MustAsync(async (model, uid, ct) =>
            {
                var db = Resolve<ICompanyDbContext>();
                return await db.Industries.AnyAsync(i => i.UId == uid, ct);
            })
            .WithMessage("Industry does not exist");
    }
}
