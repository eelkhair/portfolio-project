using System.Text.RegularExpressions;
using AdminAPI.Contracts.Models.Jobs.Requests;
using FastEndpoints;
using FluentValidation;

namespace AdminApi.Features.Jobs.DraftGenerator;

public sealed class JobGenValidator : Validator<JobGenRequest>
{
    public JobGenValidator()
    {
        RuleFor(x => x.Brief)
            .NotEmpty().WithMessage("Brief is required")
            .MinimumLength(10).WithMessage("Brief must be at least 10 characters");

        RuleFor(x => x.TitleSeed)
            .NotEmpty().WithMessage("Title Seed is required");

        RuleFor(x => x.RoleLevel)
            .IsInEnum();

        RuleFor(x => x.Tone)
            .IsInEnum();

        RuleFor(x => x.MaxBullets)
            .InclusiveBetween(3, 8);

        // Optional; allow "", "Remote", "Hybrid", or "City, ST"
        RuleFor(x => x.Location)
            .Must(IsValidLocation)
            .WithMessage("Location must be \"City, ST\", \"Remote\", \"Hybrid\", or empty");

        // Optional CSVs: trim and cap length a bit for sanity
        RuleFor(x => x.TechStackCSV).MaximumLength(2000);
        RuleFor(x => x.MustHavesCSV).MaximumLength(1000);
        RuleFor(x => x.NiceToHavesCSV).MaximumLength(1000);
    }
    private static bool IsValidLocation(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return true;
        if (Regex.IsMatch(v, "^(?i:remote|hybrid)$")) return true;
        // naive "City, ST" check
        return Regex.IsMatch(v, @"^[A-Za-z .'-]+,\s?[A-Za-z]{2}$");
    }

}