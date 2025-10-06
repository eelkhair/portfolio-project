﻿namespace AdminAPI.Contracts.Models.Jobs.Requests;

public class JobGenRequest
{
    /// <summary>Short brief of the role (what they will own / impact).</summary>
    public string Brief { get; set; } = "";

    public RoleLevel RoleLevel { get; set; } = RoleLevel.Mid;
    public Tone Tone { get; set; } = Tone.Neutral;

    
    public int MaxBullets { get; set; } = 6;
    
    public string? CompanyName { get; set; }
    public string? TeamName { get; set; }
    public string? Location { get; set; } 
    public string? TitleSeed { get; set; }
    public string? TechStackCSV { get; set; }
    public string? MustHavesCSV { get; set; }
    public string? NiceToHavesCSV { get; set; }
    public string? Benefits { get; set; }
}