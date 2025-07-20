namespace JobAPI.Contracts.Job.Responses;

public record JobResponse(int Id, Guid JobId, string Title = "", string Description = "");
