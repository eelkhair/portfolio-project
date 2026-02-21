namespace JobBoard.Monolith.Contracts.Drafts;

public class DraftRewriteResponse
{
    public string Field { get; set; } = null!;
    public List<string> Options { get; set; } = new();
    public DraftRewriteMetadata Meta { get; set; } = null!;
}

public class DraftRewriteMetadata
{
    public string? Model{get;set;}
    public int? PromptTokens{get;set;}
    public int? CompletionTokens{get;set;}
    public int? TotalTokens{get;set;}
    public string? FinishReason{get;set;}
}