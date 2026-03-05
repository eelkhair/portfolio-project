using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace JobBoard.AI.Application.Actions.Resumes.Parse;

/// <summary>
/// Shared utility for extracting plain text from resume file bytes.
/// </summary>
public static class ResumeTextExtractor
{
    public static string ExtractText(string base64Content, string contentType)
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

    public static string ExtractEmail(string text)
    {
        var match = Regex.Match(text, @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}");
        return match.Success ? match.Value : "";
    }

    public static string ExtractPhone(string text)
    {
        var match = Regex.Match(text, @"(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}");
        return match.Success ? match.Value : "";
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
