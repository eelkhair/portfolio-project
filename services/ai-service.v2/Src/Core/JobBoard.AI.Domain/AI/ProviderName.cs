namespace JobBoard.AI.Domain.AI;

public sealed record ProviderName(string Value)
{
    public static ProviderName OpenAI => new("openai");
    public static ProviderName Anthropic => new("anthropic");
}