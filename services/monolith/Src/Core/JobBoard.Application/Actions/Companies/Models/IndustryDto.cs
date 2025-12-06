using JobBoard.Application.Actions.Base;

namespace JobBoard.Application.Actions.Companies.Models;

public class IndustryDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
}