namespace JobBoard.Application.Actions.Jobs.Models;

public class JobRewriteResponse
{
    public string Field { get; set; } = null!;
    public List<string> Options { get; set; } = new();
    public JobRewriteMetadata Meta { get; set; } = null!;
}

public class JobRewriteMetadata
{
    public string Model{get;set;} = null!;
    public int PromptTokens{get;set;}
    public int CompletionTokens{get;set;}
    public int TotalTokens{get;set;}
    public string FinishReason{get;set;} = null!;
}