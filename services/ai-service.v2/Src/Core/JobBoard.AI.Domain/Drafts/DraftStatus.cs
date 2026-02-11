namespace JobBoard.AI.Domain.Drafts;

public sealed record DraftStatus(string Value)
{
    public static DraftStatus Draft => new("draft");
    public static DraftStatus Generated => new("generated");
    public static DraftStatus Finalized => new("finalized");

    public override string ToString() => Value;
}