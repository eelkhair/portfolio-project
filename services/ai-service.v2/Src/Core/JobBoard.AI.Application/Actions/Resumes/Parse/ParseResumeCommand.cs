using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.Parse;

public class ParseResumeCommand(ResumeParseRequest request) : BaseCommand<ResumeParseResponse>, ISystemCommand
{
    public ResumeParseRequest Request { get; } = request;
}

public class ParseResumeCommandHandler(
    IHandlerContext context,
    IAiPrompt<ResumeParseRequest> aiPrompt,
    IChatService chatService)
    : BaseCommandHandler(context),
      IHandler<ParseResumeCommand, ResumeParseResponse>
{
    public async Task<ResumeParseResponse> HandleAsync(ParseResumeCommand command, CancellationToken cancellationToken)
    {
        var text = ResumeTextExtractor.ExtractText(command.Request.FileContent, command.Request.ContentType);

        Logger.LogInformation("Extracted {Length} characters from resume {FileName}",
            text.Length, command.Request.FileName);

        var systemPrompt = aiPrompt.BuildSystemPrompt();
        var userPrompt = aiPrompt.BuildUserPrompt(command.Request).Replace("{RESUME_TEXT}", text);

        var result = await chatService.GetResponseAsync<ResumeParseResponse>(
            systemPrompt, userPrompt, aiPrompt.AllowTools, cancellationToken);

        // Backfill email/phone from regex if the LLM missed them
        if (string.IsNullOrEmpty(result.Email))
            result.Email = ResumeTextExtractor.ExtractEmail(text);

        if (string.IsNullOrEmpty(result.Phone))
            result.Phone = ResumeTextExtractor.ExtractPhone(text);

        Logger.LogInformation(
            "Parsed resume {FileName}: {FirstName} {LastName}, {SkillCount} skills, {WorkCount} work entries, {EduCount} education entries",
            command.Request.FileName, result.FirstName, result.LastName,
            result.Skills.Count, result.WorkHistory.Count, result.Education.Count);

        return result;
    }
}
