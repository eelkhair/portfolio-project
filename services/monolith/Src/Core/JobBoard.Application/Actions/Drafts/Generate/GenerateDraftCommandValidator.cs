using FluentValidation;

namespace JobBoard.Application.Actions.Drafts.Generate;

public class GenerateDraftCommandValidator : AbstractValidator<GenerateDraftCommand>
{
    public GenerateDraftCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Company ID is required.");

        RuleFor(x => x.Request.Brief)
            .NotEmpty().WithMessage("Brief is required.")
            .MaximumLength(2000);

        RuleFor(x => x.Request.MaxBullets)
            .InclusiveBetween(1, 20).WithMessage("MaxBullets must be between 1 and 20.");

        When(x => !string.IsNullOrWhiteSpace(x.Request.CompanyName), () =>
        {
            RuleFor(x => x.Request.CompanyName)
                .MaximumLength(250);
        });

        When(x => !string.IsNullOrWhiteSpace(x.Request.TitleSeed), () =>
        {
            RuleFor(x => x.Request.TitleSeed)
                .MaximumLength(250);
        });
    }
}
