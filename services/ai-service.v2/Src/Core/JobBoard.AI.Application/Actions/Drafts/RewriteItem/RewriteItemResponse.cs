namespace JobBoard.AI.Application.Actions.Drafts.RewriteItem;

public class RewriteItemResponse
{
    public RewriteField Field { get; set; }

    /// <summary>
    /// Exactly three rewritten options
    /// </summary>
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

    public RewriteItemResponseMeta Meta { get; init; } = new();
}

public sealed class RewriteItemResponseMeta
{
    public string Model { get; init; } = default!;

    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }

    public string? FinishReason { get; init; }
}