namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 2 prompt: extracts professional summary and skills.
/// Runs in parallel with other section parsers.
/// </summary>
public static class ParseResumeSkillsPrompt
{
    public const string JsonShape = """
                                    {
                                      "summary": "string — 2-3 sentence professional summary capturing key strengths, experience level, and primary domain",
                                      "skills": ["skill1", "skill2", "..."]
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY the professional summary and skills from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - If a field is not found, use "" for strings, [] for arrays.
                                         - Skills: extract individual skills as short tokens (e.g. "Python", "AWS", "React"), not full sentences.
                                         - Summary: generate a concise 2-3 sentence professional summary capturing the candidate's key strengths, experience level, and primary domain. This is the ONLY field you generate — all other fields are extracted verbatim.
                                         - Do NOT extract contact info, work history, education, certifications, or projects.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract professional summary and skills from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
