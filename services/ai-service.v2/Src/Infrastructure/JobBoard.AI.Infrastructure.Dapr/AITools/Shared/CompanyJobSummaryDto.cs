namespace JobBoard.AI.Infrastructure.Dapr.AITools.Shared;

public class CompanyJobSummaryDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int JobCount { get; set; }
}
