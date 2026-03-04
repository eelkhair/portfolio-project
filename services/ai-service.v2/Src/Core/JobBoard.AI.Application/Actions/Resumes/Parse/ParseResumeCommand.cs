using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

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
        var text = ExtractText(command.Request.FileContent, command.Request.ContentType);

        Logger.LogInformation("Extracted {Length} characters from resume {FileName}",
            text.Length, command.Request.FileName);

        var systemPrompt = aiPrompt.BuildSystemPrompt();
        var userPrompt = aiPrompt.BuildUserPrompt(command.Request).Replace("{RESUME_TEXT}", text);

        var result = await chatService.GetResponseAsync<ResumeParseResponse>(
            systemPrompt, userPrompt, aiPrompt.AllowTools, cancellationToken);

        // Backfill email/phone from regex if the LLM missed them
        if (string.IsNullOrEmpty(result.Email))
            result.Email = ExtractEmail(text);

        if (string.IsNullOrEmpty(result.Phone))
            result.Phone = ExtractPhone(text);

        Logger.LogInformation(
            "Parsed resume {FileName}: {FirstName} {LastName}, {SkillCount} skills, {WorkCount} work entries, {EduCount} education entries",
            command.Request.FileName, result.FirstName, result.LastName,
            result.Skills.Count, result.WorkHistory.Count, result.Education.Count);

        return result;
    }

    private static string ExtractEmail(string text)
    {
        var match = Regex.Match(text, @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}");
        return match.Success ? match.Value : "";
    }

    private static string ExtractPhone(string text)
    {
        var match = Regex.Match(text, @"(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}");
        return match.Success ? match.Value : "";
    }

    private static string ExtractText(string base64Content, string contentType)
    {
        var bytes = Convert.FromBase64String(base64Content);
        using var stream = new MemoryStream(bytes);

        return contentType switch
        {
            "application/pdf" => ExtractPdf(stream),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractDocx(stream),
            "text/plain" => ExtractPlainText(stream),
            _ => throw new NotSupportedException($"Unsupported content type: {contentType}")
        };
    }

    private static string ExtractPdf(Stream stream)
    {
        using var doc = PdfDocument.Open(stream);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }

    private static string ExtractDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body is null) return "";

        return string.Join("\n", body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
            .Select(p => p.InnerText));
    }

    private static string ExtractPlainText(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
