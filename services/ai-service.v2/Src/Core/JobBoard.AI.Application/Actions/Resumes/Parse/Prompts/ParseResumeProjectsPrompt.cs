namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 2 prompt: extracts projects only.
/// </summary>
public static class ParseResumeProjectsPrompt
{
    public const string JsonShape = """
                                    {
                                      "projects": [
                                        {
                                          "name": "string",
                                          "description": "string or null",
                                          "technologies": ["string"],
                                          "url": "string or null"
                                        }
                                      ]
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY projects (portfolio projects, side projects, or open-source contributions) from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - Include technologies used if listed.
                                         - If no projects are found, return an empty array.
                                         - Do NOT extract contact info, skills, work history, education, or certifications.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract projects from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
