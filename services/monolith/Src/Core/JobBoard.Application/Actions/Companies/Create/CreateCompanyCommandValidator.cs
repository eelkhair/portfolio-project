using FluentValidation;
using JobBoard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Application.Actions.Companies.Create;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator(IJobBoardDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(250);

        RuleFor(c => c.CompanyEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);

        RuleFor(c => c.CompanyWebsite)
            .MaximumLength(200)
            .Must(url => string.IsNullOrWhiteSpace(url) 
                         || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Company website is not a valid URL");

        RuleFor(c => c.IndustryUId)
            .NotEmpty().WithMessage("Industry is required");

        RuleFor(c => c).CustomAsync(async (model, ctx, ct) =>
        {
            var baseQuery = context.Companies
                .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue);

            var nameExists = await baseQuery
                .AnyAsync(x => x.Name == model.Name, ct);

            if (nameExists)
                ctx.AddFailure(nameof(model.Name), "Company name already exists");

            var emailExists = await baseQuery
                .AnyAsync(x => x.Email == model.CompanyEmail, ct);

            if (emailExists)
                ctx.AddFailure(nameof(model.CompanyEmail), "Company email already exists");

            if (model.IndustryUId != Guid.Empty)
            {
                var industryExists = await context.Industries
                    .Where(i => EF.Property<DateTime>(i, "PeriodEnd") == DateTime.MaxValue)
                    .AnyAsync(i => i.UId == model.IndustryUId, ct);

                if (!industryExists)
                    ctx.AddFailure(nameof(model.IndustryUId), "Industry does not exist");
            }
        });
    }
}
