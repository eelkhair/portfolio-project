namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 1 prompt: extracts contact info, professional summary, and skills only.
/// Designed for fast TTFT (~1-2 seconds).
/// </summary>
public static class ParseResumeQuickPrompt
{
    public const string JsonShape = """
                                    {
                                      "firstName": "string",
                                      "lastName": "string",
                                      "email": "string (or empty)",
                                      "phone": "string (or empty)",
                                      "linkedin": "string — full URL or empty",
                                      "portfolio": "string — full URL or empty",
                                      "summary": "string — 2-3 sentence professional summary capturing key strengths, experience level, and primary domain",
                                      "skills": ["skill1", "skill2", "..."]
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY contact information and skills from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - If a field is not found, use "" for strings, [] for arrays.
                                         - Skills: extract individual skills as short tokens (e.g. "Python", "AWS", "React"), not full sentences.
                                         - LinkedIn/portfolio: include full URL if present. If only a username/handle, prefix with the appropriate domain.
                                         - Phone: normalize to a consistent format if possible, otherwise keep as-is.
                                         - Summary: generate a concise 2-3 sentence professional summary capturing the candidate's key strengths, experience level, and primary domain. This is the ONLY field you generate — all other fields are extracted verbatim.
                                         - Do NOT extract work history, education, certifications, or projects. Those will be extracted separately.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract contact info, summary, and skills from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
