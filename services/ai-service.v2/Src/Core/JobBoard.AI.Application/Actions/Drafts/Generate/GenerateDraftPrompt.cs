using JobBoard.AI.Application.Actions.Base;

namespace JobBoard.AI.Application.Actions.Drafts.Generate;

public class GenerateDraftPrompt : IAiPrompt<GenerateDraftRequest>
{
    public bool AllowTools => false;
    private const string JobGenJsonShape = """
                                           {
                                             "title": "string (<= 80 chars; no [Hiring] or emojis)",
                                             "aboutRole": "2-4 short paragraphs; omit salary/benefits if not provided",
                                             "responsibilities": ["bullet 1", "bullet 2", "... up to MAX_BULLETS"],
                                             "qualifications": ["bullet 1", "bullet 2", "... up to MAX_BULLETS"],
                                             "notes": "explicit caveats; if none, use \"\"",
                                             "location": "City, ST | Remote | Hybrid | \"\" if unknown",
                                             "metadata": {
                                               "roleLevel": "ROLE_LEVEL",
                                               "tone": "TONE"
                                             }
                                           }
                                           """;
    public string Name => "GenerateJob";
    public string Version => "0.1";
    public string BuildUserPrompt(GenerateDraftRequest request)
    {
        var jsonShape = JobGenJsonShape
            .Replace("MAX_BULLETS", request.MaxBullets.ToString())
            .Replace("ROLE_LEVEL", request.RoleLevel.ToString().ToLowerInvariant())
            .Replace("TONE", request.Tone.ToString().ToLowerInvariant());

        return $"""
                Brief:
                {request.Brief}

                Company signals (optional):
                - Company: {request.CompanyName ?? ""}
                - Team: {request.TeamName ?? ""}
                - Location: {request.Location ?? ""}
                - Job title seed: {request.TitleSeed ?? ""}
                - Tech stack: {request.TechStackCsv ?? ""}
                - Must-haves: {request.MustHavesCsv ?? ""}
                - Nice-to-haves: {request.NiceToHavesCsv ?? ""}
                - Benefits summary: {request.Benefits ?? "omit"}

                Generation settings:
                - Tone: {request.Tone}
                - Role level: {request.RoleLevel}
                - Max bullets per list: {request.MaxBullets}

                Return JSON ONLY in this shape:
                {jsonShape}
                """;
    }

    public string BuildSystemPrompt()
    {
        return """
               You are an expert recruiting copywriter.
               Write inclusive, legally safe job postings without bias or unverifiable claims.

               HARD RULES
               - Audience: candidates scanning quickly.
               - Clarity > cleverness. Short sentences; use bullet lists.
               - Avoid age, gender, nationality, or health-related requirements unless explicitly provided.
               - Do NOT invent salary, benefits, or location. If unknown, omit.
               - Normalize US location to "City, ST"; allow Remote/Hybrid if stated.
               - No placeholders like "TBD".
               - Responsibilities start with action verbs ("Design", "Build", "Own").
               - Role level: junior|mid|senior|staff|principal. Tone: neutral|concise|friendly.
               - Output JSON ONLY. Match schema exactly (no markdown, no extra keys).
               - If there are no caveats, set "notes" to "" (empty string).
               - Always include "location". Normalize to "City, ST", or "Remote"/"Hybrid" if stated. If unknown, set location to "" (empty string).
               - You do not call tools or request additional data. Generate output only from the provided input.
               - Prompt version: 0.1. Scope: job draft generation only.
               """.Trim();
    }
}