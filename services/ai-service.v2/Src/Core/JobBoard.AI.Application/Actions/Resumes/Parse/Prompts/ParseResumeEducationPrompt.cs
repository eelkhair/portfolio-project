namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 2 prompt: extracts education only.
/// </summary>
public static class ParseResumeEducationPrompt
{
    public const string JsonShape = """
                                    {
                                      "education": [
                                        {
                                          "institution": "string",
                                          "degree": "string (e.g. B.S., M.S., Ph.D., MBA)",
                                          "fieldOfStudy": "string or null",
                                          "startDate": "YYYY-MM-DD",
                                          "endDate": "YYYY-MM-DD or null"
                                        }
                                      ]
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY education history from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - Dates: normalize to ISO 8601 (YYYY-MM-DD). If only year is given, use YYYY-01-01. If month and year, use YYYY-MM-01.
                                         - Order by most recent first (descending startDate).
                                         - If no education is found, return an empty array.
                                         - Do NOT extract contact info, skills, work history, certifications, or projects.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract education from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
