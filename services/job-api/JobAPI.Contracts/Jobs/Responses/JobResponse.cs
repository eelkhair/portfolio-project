namespace JobAPI.Contracts.Jobs.Responses;

public record JobResponse(int Id, Guid JobId, string Title = "", string Description = "");
