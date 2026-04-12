using FluentValidation;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Application.Actions.Applications.Admin;

public class UpdateApplicationStatusCommandValidator : AbstractValidator<UpdateApplicationStatusCommand>
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedTransitions = new()
    {
        [ApplicationStatus.Submitted] = [ApplicationStatus.UnderReview, ApplicationStatus.Shortlisted, ApplicationStatus.Rejected],
        [ApplicationStatus.UnderReview] = [ApplicationStatus.Shortlisted, ApplicationStatus.Rejected],
        [ApplicationStatus.Shortlisted] = [ApplicationStatus.Accepted, ApplicationStatus.Rejected],
        [ApplicationStatus.Rejected] = [ApplicationStatus.UnderReview],
        [ApplicationStatus.Accepted] = []
    };

    public UpdateApplicationStatusCommandValidator(IJobBoardQueryDbContext context)
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => Enum.TryParse<ApplicationStatus>(s, true, out _))
            .WithMessage("Invalid application status");

        RuleFor(x => x.ApplicationId)
            .NotEmpty().WithMessage("Application ID is required");

        RuleFor(x => x).CustomAsync(async (command, ctx, ct) =>
        {
            if (!Enum.TryParse<ApplicationStatus>(command.Status, true, out var targetStatus))
                return;

            var app = await context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == command.ApplicationId, ct);

            if (app is null)
            {
                ctx.AddFailure(nameof(command.ApplicationId), "Application not found");
                return;
            }

            if (app.Status == targetStatus)
                return;

            var allowed = AllowedTransitions.GetValueOrDefault(app.Status, []);
            if (!allowed.Contains(targetStatus))
            {
                ctx.AddFailure(nameof(command.Status),
                    $"Cannot transition from {app.Status} to {targetStatus}. " +
                    $"Allowed: {(allowed.Length > 0 ? string.Join(", ", allowed) : "none (terminal state)")}");
            }
        });
    }
}
