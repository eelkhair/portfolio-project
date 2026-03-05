namespace JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;

/// <summary>
/// Phase 2 prompt: extracts certifications only.
/// </summary>
public static class ParseResumeCertificationsPrompt
{
    public const string JsonShape = """
                                    {
                                      "certifications": [
                                        {
                                          "name": "string",
                                          "issuingOrganization": "string or null",
                                          "issueDate": "YYYY-MM-DD or null",
                                          "expirationDate": "YYYY-MM-DD or null",
                                          "credentialId": "string or null"
                                        }
                                      ]
                                    }
                                    """;

    public static string SystemPrompt => """
                                         You are an expert resume parser. Extract ONLY certifications from resume text.

                                         HARD RULES
                                         - Extract ONLY information explicitly present in the resume. Never invent or guess.
                                         - Dates: normalize to ISO 8601 (YYYY-MM-DD). If only year is given, use YYYY-01-01. If month and year, use YYYY-MM-01.
                                         - If no certifications are found, return an empty array.
                                         - Do NOT extract contact info, skills, work history, education, or projects.
                                         - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
                                         """.Trim();

    public static string BuildUserPrompt(ResumeParseRequest request) =>
        string.Join("\n",
            "Extract certifications from the following resume.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
}
