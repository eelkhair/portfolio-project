using FluentValidation;

namespace JobBoard.Application.Actions.Settings.Provider;

public class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(x => x.Request.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .MaximumLength(50);

        RuleFor(x => x.Request.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(100);
    }
}
