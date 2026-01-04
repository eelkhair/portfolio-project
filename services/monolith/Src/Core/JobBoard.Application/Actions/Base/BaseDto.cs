
namespace JobBoard.Application.Actions.Base;

public class BaseDto
{

    public Guid Id { get; set; }
    public Guid UId => Id;

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}