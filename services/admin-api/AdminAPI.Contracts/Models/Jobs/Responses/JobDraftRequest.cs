﻿namespace AdminAPI.Contracts.Models.Jobs.Responses;

public class JobDraftResponse
{
    public string Title { get; set; } = "";
    public string AboutRole { get; set; } = "";
    public List<string> Responsibilities { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
    public string Notes { get; set; } = "";
    public string Location { get; set; } = "";
    public JobGenMetadata Metadata { get; set; } = new();
    public string? Id { get; set; } = "";
}