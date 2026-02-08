using JobBoard.AI.Application.Actions.Shared;

namespace JobBoard.AI.Application.Actions.GenerateJob;

public class GenerateJobRequest
{
    public string Brief { get; set; } = "";

    public RoleLevel RoleLevel { get; set; } = RoleLevel.Mid;
    public Tone Tone { get; set; } = Tone.Neutral;
    
    public int MaxBullets { get; set; } = 6;
    
    public string? CompanyName { get; set; }
    public string? TeamName { get; set; }
    public string? Location { get; set; } 
    public string? TitleSeed { get; set; }
    public string? TechStackCsv { get; set; }
    public string? MustHavesCsv { get; set; }
    public string? NiceToHavesCsv { get; set; }
    public string? Benefits { get; set; }
}