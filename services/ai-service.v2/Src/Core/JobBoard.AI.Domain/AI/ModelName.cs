namespace JobBoard.AI.Domain.AI;

public sealed record ModelName(string Value)
{
    public static ModelName Gpt41Mini => new("gpt-4.1-mini");
}