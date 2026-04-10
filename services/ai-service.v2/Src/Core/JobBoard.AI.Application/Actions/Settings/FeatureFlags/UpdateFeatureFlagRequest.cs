namespace JobBoard.AI.Application.Actions.Settings.FeatureFlags;

public class UpdateFeatureFlagRequest
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
