namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 1 prompt: extracts contact info only (name, email, phone, links).
/// Designed to be very fast (~1 second) so Phase 2 can start immediately.
/// </summary>
public static class ParseResumeContactPrompt
{
    public const string JsonShape = """
                                    {
                                      "firstName": "string",
                                      "lastName": "string",
                                      "email": "string (or empty)",
                                      "phone": "string (or empty)",
                                      "linkedin": "string — full URL or empty",
                                      "portfolio": "string — full URL or empty"
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY contact information from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - If a field is not found, use "" for strings.
                                         - LinkedIn/portfolio: include full URL if present. If only a username/handle, prefix with the appropriate domain.
                                         - Phone: normalize to a consistent format if possible, otherwise keep as-is.
                                         - Do NOT extract skills, summary, work history, education, certifications, or projects.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract contact info from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
