using FluentValidation;

namespace JobBoard.Application.Actions.Jobs.Drafts;

public class RewriteDraftItemCommandValidator : AbstractValidator<RewriteDraftItemCommand>
{
    public RewriteDraftItemCommandValidator()
    {
        RuleFor(x => x.DraftItemRewriteRequest.Field)
            .NotEmpty().WithMessage("Field is required.")
            .MaximumLength(100);

        RuleFor(x => x.DraftItemRewriteRequest.Value)
            .NotEmpty().WithMessage("Value is required.")
            .MaximumLength(5000);

        RuleFor(x => x.DraftItemRewriteRequest.Context)
            .NotNull().WithMessage("Context is required.");

        RuleFor(x => x.DraftItemRewriteRequest.Style)
            .NotNull().WithMessage("Style is required.");
    }
}
