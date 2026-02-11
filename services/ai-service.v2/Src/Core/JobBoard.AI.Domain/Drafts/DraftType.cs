namespace JobBoard.AI.Domain.Drafts;

public sealed record DraftType(string Value)
{
    public static DraftType Job => new("job");
    public static DraftType Company => new("company");
    public static DraftType Rewrite => new("rewrite");

    public override string ToString() => Value;
}