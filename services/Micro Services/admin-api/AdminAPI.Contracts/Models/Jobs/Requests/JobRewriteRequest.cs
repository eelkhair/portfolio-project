namespace AdminAPI.Contracts.Models.Jobs.Requests;

public class JobRewriteRequest
{
    public string Field { get; set; } = null!;
    public string Value {get;set;} = null!;
    public Dictionary<string, object> Context{get; set;} = null!;
    public Dictionary<string, object> Style{get; set;} = null!;
}