namespace JobBoard.HealthChecks.Dtos;

public class KeycloakOptions
{
    public string Authority { get; set; } = string.Empty;
    public string[] ClientIds { get; set; } = [];
}
