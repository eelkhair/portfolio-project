using FluentValidation;
using JobBoard.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobBoard.Application.Actions.Companies.Create;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator(IJobBoardDbContext context, ILogger<CreateCompanyCommandValidator> logger)
    {
        logger.LogInformation("Validating {ClassName}", nameof(CreateCompanyCommandValidator));
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
            logger.LogInformation("Checking uniqueness of company {CompanyName}", model.Name);
            var baseQuery = context.Companies
                .Where(x => EF.Property<DateTime>(x, "PeriodEnd") == DateTime.MaxValue);

            var nameExists = await baseQuery
                .AnyAsync(x => x.Name == model.Name, ct);

            if (nameExists)
                ctx.AddFailure(nameof(model.Name), "Company name already exists");

            logger.LogInformation("Checking uniqueness of company email {CompanyEmail}", model.CompanyEmail);
            var emailExists = await baseQuery
                .AnyAsync(x => x.Email == model.CompanyEmail, ct);

            if (emailExists)
                ctx.AddFailure(nameof(model.CompanyEmail), "Company email already exists");

            
            logger.LogInformation("Checking uniqueness of admin email {AdminEmail}", model.AdminEmail);
            var userEmailExists =  await context.Users
                .Where(i => EF.Property<DateTime>(i, "PeriodEnd") == DateTime.MaxValue)

                .AnyAsync(x => x.Email == model.AdminEmail, ct);

            if (userEmailExists)
                ctx.AddFailure(nameof(model.AdminEmail), "Admin email already exists");
           
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
