namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 2 prompt: extracts work history only.
/// </summary>
public static class ParseResumeWorkHistoryPrompt
{
    public const string JsonShape = """
                                    {
                                      "workHistory": [
                                        {
                                          "company": "string",
                                          "jobTitle": "string",
                                          "startDate": "YYYY-MM-DD",
                                          "endDate": "YYYY-MM-DD or null if current",
                                          "description": "string — preserve ALL bullet points verbatim, joined by newlines, or null if none",
                                          "isCurrent": true/false
                                        }
                                      ]
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY work history from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - Dates: normalize to ISO 8601 (YYYY-MM-DD). If only year is given, use YYYY-01-01. If month and year, use YYYY-MM-01.
                                         - Set isCurrent=true and endDate=null if the role says "Present", "Current", or has no end date.
                                         - Preserve ALL bullet points / responsibilities exactly as written. Join them with newline characters. Do NOT summarize, condense, or rephrase.
                                         - Order by most recent first (descending startDate).
                                         - If no work history is found, return an empty array.
                                         - Do NOT extract contact info, skills, education, certifications, or projects.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract work history from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
