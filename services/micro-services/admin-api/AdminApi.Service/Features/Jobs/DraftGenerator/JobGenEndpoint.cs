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
            CompanyName = req.CompanyName,
            TeamName = req.TeamName,
            Location = req.Location?.Trim(),
            TitleSeed = req.TitleSeed,
            TechStackCSV = req.TechStackCSV,
            MustHavesCSV = req.MustHavesCSV,
            NiceToHavesCSV = req.NiceToHavesCSV,
            Benefits = req.Benefits
        };

        var result = await _ai.GenerateJobAsync(companyId, normalized, ct);

        await Send.OkAsync(result, ct);
    }
}