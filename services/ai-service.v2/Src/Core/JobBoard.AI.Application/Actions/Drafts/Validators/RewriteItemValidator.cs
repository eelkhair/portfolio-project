using FluentValidation;
using JobBoard.AI.Application.Actions.Drafts.RewriteItem;

namespace JobBoard.AI.Application.Actions.Drafts.Validators;

public sealed class RewriteItemValidator 
    : AbstractValidator<RewriteItemRequest>
{
    public RewriteItemValidator()
    {
        // -----------------------------
        // field
        // -----------------------------
        RuleFor(x => x.Field)
            .IsInEnum();

        // -----------------------------
        // value
        // -----------------------------
        RuleFor(x => x.Value)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(5_000);

        // -----------------------------
        // context (optional)
        // -----------------------------
        When(x => x.Context is not null, () =>
        {
            RuleFor(x => x.Context!.Title)
                .MinimumLength(3)
                .MaximumLength(160)
                .When(x => x.Context!.Title is not null);

            RuleFor(x => x.Context!.AboutRole)
                .MinimumLength(10)
                .MaximumLength(10_000)
                .When(x => x.Context!.AboutRole is not null);

            RuleForEach(x => x.Context!.Responsibilities!)
                .MinimumLength(3)
                .When(x => x.Context!.Responsibilities is not null);

            RuleForEach(x => x.Context!.Qualifications!)
                .MinimumLength(3)
                .When(x => x.Context!.Qualifications is not null);

            RuleFor(x => x.Context!.CompanyName)
                .MaximumLength(160)
                .When(x => x.Context!.CompanyName is not null);
        });

        // -----------------------------
        // style (optional)
        // -----------------------------
        When(x => x.Style is not null, () =>
        {
            RuleFor(x => x.Style!.Audience)
                .MaximumLength(120)
                .When(x => x.Style!.Audience is not null);

            RuleFor(x => x.Style!.MaxWords)
                .InclusiveBetween(10, 2_000)
                .When(x => x.Style!.MaxWords.HasValue);

            RuleFor(x => x.Style!.NumParagraphs)
                .InclusiveBetween(1, 4)
                .When(x => x.Style!.NumParagraphs.HasValue);

            RuleFor(x => x.Style!.BulletsPerSection)
                .InclusiveBetween(3, 12)
                .When(x => x.Style!.BulletsPerSection.HasValue);

            RuleFor(x => x.Style!.Language)
                .Matches("^[a-z]{2,5}$")
                .WithMessage("language must be ISO code like 'en'")
                .When(x => x.Style!.Language is not null);

            RuleForEach(x => x.Style!.AvoidPhrases!)
                .MinimumLength(2)
                .When(x => x.Style!.AvoidPhrases is not null);
        });

        // -----------------------------
        // superRefine equivalent
        // -----------------------------
        RuleFor(x => x)
            .Custom((req, ctx) =>
            {
                if (req.Field == RewriteField.AboutRole)
                {
                    var companyName = req.Context?.CompanyName?.Trim();
                    if (string.IsNullOrEmpty(companyName))
                    {
                        ctx.AddFailure(
                            "Context.CompanyName",
                            "context.companyName is required when field is 'aboutRole'");
                    }
                }
            });
    }
}
