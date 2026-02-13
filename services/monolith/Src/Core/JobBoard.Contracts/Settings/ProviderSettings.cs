namespace JobBoard.Monolith.Contracts.Settings;

public class ProviderSettings
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4.1-mini";
}
