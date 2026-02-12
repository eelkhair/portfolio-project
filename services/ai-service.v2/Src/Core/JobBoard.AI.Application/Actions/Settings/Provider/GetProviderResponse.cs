namespace JobBoard.AI.Application.Actions.Settings.Provider;

public class GetProviderResponse
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4.1-mini";
}
