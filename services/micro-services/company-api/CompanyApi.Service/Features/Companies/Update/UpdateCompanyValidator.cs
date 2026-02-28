using CompanyAPI.Contracts.Models.Companies.Requests;
using CompanyApi.Infrastructure.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Features.Companies.Update;

public class UpdateCompanyValidator : Validator<UpdateCompanyRequest>
{
    public UpdateCompanyValidator()
    {
        RuleFor(c => c.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(250).WithMessage("Company name cannot exceed 250 characters");

        RuleFor(c => c.CompanyEmail)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company email is required")
            .EmailAddress().WithMessage("Company email is not valid")
            .MaximumLength(100).WithMessage("Company email cannot exceed 100 characters");

        RuleFor(c => c.CompanyWebsite)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(200).WithMessage("Company website cannot exceed 200 characters")
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Company website is not a valid URL");

        RuleFor(c => c.IndustryUId)
            .NotEmpty().WithMessage("Industry is required");

        RuleFor(c => c).CustomAsync(async (model, ctx, ct) =>
        {
            var db = Resolve<ICompanyDbContext>();
            var httpContext = Resolve<IHttpContextAccessor>().HttpContext!;
            var companyUId = Guid.Parse(httpContext.Request.RouteValues["id"]!.ToString()!);

            var dup = await db.Companies
                .Where(x => x.UId != companyUId && (x.Name == model.Name || x.Email == model.CompanyEmail))
                .Select(x => new { x.Name, x.Email })
                .ToListAsync(ct);

            if (dup.Any(d => d.Name == model.Name))
                ctx.AddFailure(nameof(model.Name), "Company name already exists");

            if (dup.Any(d => d.Email == model.CompanyEmail))
                ctx.AddFailure(nameof(model.CompanyEmail), "Company email already exists");

            if (model.IndustryUId != Guid.Empty)
            {
                var industryExists = await db.Industries
                    .AnyAsync(i => i.UId == model.IndustryUId, ct);

                if (!industryExists)
                    ctx.AddFailure(nameof(model.IndustryUId), "Industry does not exist");
            }
        });
    }
}
