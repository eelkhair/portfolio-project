using AdminApi.Application.Commands.Interfaces;
using AdminAPI.Contracts.Models.Jobs.Requests;
using AdminAPI.Contracts.Models.Jobs.Responses;
using Elkhair.Dev.Common.Application;
using FastEndpoints;

namespace AdminApi.Features.Jobs.DraftGenerator;

public sealed class JobGenEndpoint(IOpenAICommandService _ai)
    : Endpoint<JobGenRequest, ApiResponse<JobGenResponse>>
{
    
    private const string RouteTemplate = "jobs/{companyId}/generate";
    public override void Configure()
    {
        Post(RouteTemplate);
        AllowAnonymous(); // or RequireAuthorization()
        Summary(s =>
        {
            s.Summary = "Generate a structured job draft via AI";
            s.Description = "Returns title, aboutRole, bullets, notes, location, and metadata.";
        });
    }
    
    public override async Task HandleAsync(JobGenRequest req, CancellationToken ct)
    {
        var companyId = Route<string>("companyId")!;

        // Normalize UI-friendly enums to the lowercase strings your model expects (if needed in your service)
        var normalized = new JobGenRequest
        {
            Brief = req.Brief,
            RoleLevel = req.RoleLevel,   // keep enum; service can ToString().ToLowerInvariant()
            Tone = req.Tone,
            MaxBullets = req.MaxBullets,
            CompanyName = EmptyToNull(req.CompanyName),
            TeamName = EmptyToNull(req.TeamName),
            Location = req.Location?.Trim(),
            TitleSeed = EmptyToNull(req.TitleSeed),
            TechStackCSV = EmptyToNull(req.TechStackCSV),
            MustHavesCSV = EmptyToNull(req.MustHavesCSV),
            NiceToHavesCSV = EmptyToNull(req.NiceToHavesCSV),
            Benefits = EmptyToNull(req.Benefits)
        };

        var result = await _ai.GenerateJobAsync(companyId, normalized, ct);

        await SendOkAsync(result, ct);
    }

    private static string? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

}