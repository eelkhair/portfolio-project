namespace JobBoard.AI.Application.Actions.Settings;

public class GetProviderResponse
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4.1-mini";
}
