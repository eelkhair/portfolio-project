using JobBoard.AI.Application.Actions.Base;

namespace JobBoard.AI.Application.Actions.Resumes.Parse;

public class ParseResumePrompt : IAiPrompt<ResumeParseRequest>
{
    public string Name => "ParseResume";
    public string Version => "0.1";
    public bool AllowTools => false;

    private const string JsonShape = """
                                     {
                                       "firstName": "string",
                                       "lastName": "string",
                                       "email": "string (or empty)",
                                       "phone": "string (or empty)",
                                       "linkedin": "string — full URL or empty",
                                       "portfolio": "string — full URL or empty",
                                       "skills": ["skill1", "skill2", "..."],
                                       "workHistory": [
                                         {
                                           "company": "string",
                                           "jobTitle": "string",
                                           "startDate": "YYYY-MM-DD",
                                           "endDate": "YYYY-MM-DD or null if current",
                                           "description": "string — preserve ALL bullet points verbatim, joined by newlines, or null if none",
                                           "isCurrent": true/false
                                         }
                                       ],
                                       "education": [
                                         {
                                           "institution": "string",
                                           "degree": "string (e.g. B.S., M.S., Ph.D., MBA)",
                                           "fieldOfStudy": "string or null",
                                           "startDate": "YYYY-MM-DD",
                                           "endDate": "YYYY-MM-DD or null"
                                         }
                                       ],
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

    public string BuildSystemPrompt()
    {
        return """
               You are an expert resume parser. Extract structured data from resume text.

               HARD RULES
               - Extract ONLY information explicitly present in the resume. Never invent or guess.
               - If a field is not found, use "" for strings, [] for arrays, null for optional fields.
               - Dates: normalize to ISO 8601 (YYYY-MM-DD). If only year is given, use YYYY-01-01. If month and year, use YYYY-MM-01.
               - For work history: set isCurrent=true and endDate=null if the role says "Present", "Current", or has no end date.
               - For work history descriptions: preserve ALL bullet points / responsibilities exactly as written. Join them with newline characters. Do NOT summarize, condense, or rephrase.
               - Order work history and education by most recent first (descending startDate).
               - Skills: extract individual skills as short tokens (e.g. "Python", "AWS", "React"), not full sentences.
               - LinkedIn/portfolio: include full URL if present. If only a username/handle, prefix with the appropriate domain.
               - Phone: normalize to a consistent format if possible, otherwise keep as-is.
               - Output JSON ONLY. No markdown fences, no commentary, no extra keys.
               - Prompt version: 0.1. Scope: resume parsing only.
               """.Trim();
    }

    public string BuildUserPrompt(ResumeParseRequest request)
    {
        return string.Join("\n",
            "Parse the following resume text and return structured JSON.",
            $"File: {request.FileName} ({request.ContentType})",
            "",
            "--- RESUME TEXT ---",
            "{RESUME_TEXT}",
            "--- END RESUME TEXT ---",
            "",
            "Return JSON ONLY in this exact shape:",
            JsonShape);
    }
}
