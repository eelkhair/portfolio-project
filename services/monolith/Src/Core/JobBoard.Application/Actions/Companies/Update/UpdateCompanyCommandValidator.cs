using FluentValidation;
using JobBoard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Update;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator(IJobBoardDbContext context, ILogger<UpdateCompanyCommandValidator> logger)
    {
        logger.LogInformation("Validating {ClassName}", nameof(UpdateCompanyCommandValidator));

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Company Id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(250);

        RuleFor(c => c.CompanyEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);

        When(c => !string.IsNullOrWhiteSpace(c.CompanyWebsite), () =>
        {
            RuleFor(c => c.CompanyWebsite)
                .MaximumLength(200)
                .Must(url => string.IsNullOrWhiteSpace(url)
                             || Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Company website is not a valid URL");
        });

        RuleFor(c => c.IndustryUId)
            .NotEmpty().WithMessage("Industry is required");

        RuleFor(c => c).CustomAsync(async (model, ctx, ct) =>
        {
            var baseQuery = context.Companies
                .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue);

            logger.LogInformation("Checking uniqueness of company name {CompanyName} (excluding {CompanyId})", model.Name, model.Id);
            var nameExists = await baseQuery
                .AnyAsync(x => x.Name == model.Name && x.Id != model.Id, ct);

            if (nameExists)
                ctx.AddFailure(nameof(model.Name), "Company name already exists");

            logger.LogInformation("Checking uniqueness of company email {CompanyEmail} (excluding {CompanyId})", model.CompanyEmail, model.Id);
            var emailExists = await baseQuery
                .AnyAsync(x => x.Email == model.CompanyEmail && x.Id != model.Id, ct);

            if (emailExists)
                ctx.AddFailure(nameof(model.CompanyEmail), "Company email already exists");

            if (model.IndustryUId != Guid.Empty)
            {
                logger.LogInformation("Checking existence of industry {IndustryUId}", model.IndustryUId);
                var industryExists = await context.Industries
                    .Where(i => EF.Property<DateTime>(i, "PeriodEnd") == DateTime.MaxValue)
                    .AnyAsync(i => i.Id == model.IndustryUId, ct);

                if (!industryExists)
                    ctx.AddFailure(nameof(model.IndustryUId), "Industry does not exist");
            }
        });
    }
}
