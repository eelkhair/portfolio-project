namespace AdminAPI.Contracts.Models.Dashboard;

public class DashboardResponse
{
    public int JobCount { get; set; }
    public int CompanyCount { get; set; }
    public int DraftCount { get; set; }
    public List<LabelCountItem> JobsByType { get; set; } = [];
    public List<LabelCountItem> JobsByLocation { get; set; } = [];
    public List<LabelCountItem> TopCompanies { get; set; } = [];
    public List<RecentJobItem> RecentJobs { get; set; } = [];
}

public class LabelCountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentJobItem
{
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
