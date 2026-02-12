using JobBoard.AI.Application.Actions.Base;

namespace JobBoard.AI.Application.Actions.Drafts.RewriteItem;

public sealed class RewriteItemPrompt : IAiPrompt<RewriteItemRequest>
{
    private const string RewriteJsonShape = """
    {
      "options": [
        "string (min 3 chars)",
        "string (min 3 chars)",
        "string (min 3 chars)"
      ]
    }
    """;

    public string Name => "RewriteItem";
    public string Version => "0.1";

    public string BuildUserPrompt(RewriteItemRequest request)
    {
        var styleLines = BuildStyleHints(request.Style);
        var contextLines = BuildContext(request.Context);
        var companyRule = BuildCompanyRule(request);

        return $"""
        Rewrite the following {request.Field} item.

        Produce EXACTLY three distinct options.
        Improve clarity, inclusivity, and parallel structure.
        Preserve factual meaning.
        Do NOT add salary, benefits, or company claims not present in context.

        {companyRule}

        STRUCTURE
        - Each option must be self-contained
        - Ready to display in UI
        - No markdown
        - No numbering

        {styleLines}

        {contextLines}

        ITEM
        {request.Value}

        Return JSON ONLY in this shape:
        {RewriteJsonShape}
        """;
    }

    public string BuildSystemPrompt()
    {
        return """
        You are a precise job-content editor.

        HARD RULES
        - Follow instructions exactly.
        - Output JSON ONLY. No prose, no markdown.
        - Match the schema exactly (no extra keys).
        - Do not hallucinate missing facts.
        - Do not add company names unless explicitly provided.
        - Do not repeat the input verbatim.
        - Avoid biased or exclusionary language.
        - Use professional, inclusive wording.

        """.Trim();
    }

    private static string BuildCompanyRule(RewriteItemRequest request)
    {
        if (request.Field == RewriteField.AboutRole &&
            !string.IsNullOrWhiteSpace(request.Context?.CompanyName))
        {
            var name = request.Context.CompanyName;
            return $"""
            COMPANY REQUIREMENT
            - Every option MUST explicitly mention "{name}".
            - Example forms: "at {name}", "{name} is seeking", "{name} builds..."
            """;
        }

        return """
        COMPANY RULE
        - Do NOT add or fabricate company names.
        """;
    }

    private static string BuildStyleHints(RewriteItemStyle? style)
    {
        if (style is null) return "STYLE\n- (none)";

        var lines = new List<string>();

        if (style.Tone is not null) lines.Add($"Tone: {style.Tone}");
        if (style.Formality is not null) lines.Add($"Formality: {style.Formality}");
        if (!string.IsNullOrWhiteSpace(style.Audience)) lines.Add($"Audience: {style.Audience}");
        if (style.MaxWords is not null) lines.Add($"Max words: {style.MaxWords}");
        if (style.NumParagraphs is not null) lines.Add($"Number of paragraphs: {style.NumParagraphs}");
        if (!string.IsNullOrWhiteSpace(style.Language)) lines.Add($"Language: {style.Language}");
        if (style.BulletsPerSection is not null) lines.Add($"Bullets per section: {style.BulletsPerSection}");
        if (style.IncludeEEOBoilerplate == true)
            lines.Add("Include brief EEO-friendly phrasing when appropriate");

        if (style.AvoidPhrases?.Any() == true)
            lines.Add($"Avoid phrases: {string.Join(", ", style.AvoidPhrases)}");

        return $"""
        STYLE
        - {string.Join("\n- ", lines)}
        """;
    }

    private static string BuildContext(RewriteItemContext? context)
    {
        if (context is null) return "CONTEXT\n- (none)";

        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(context.Title))
            lines.Add($"title: {context.Title}");

        if (!string.IsNullOrWhiteSpace(context.AboutRole))
            lines.Add($"aboutRole: {context.AboutRole}");

        if (context.Responsibilities?.Any() == true)
            lines.Add($"responsibilities: {string.Join(" | ", context.Responsibilities)}");

        if (context.Qualifications?.Any() == true)
            lines.Add($"qualifications: {string.Join(" | ", context.Qualifications)}");

        if (!string.IsNullOrWhiteSpace(context.CompanyName))
            lines.Add($"companyName: {context.CompanyName}");

        return $"""
        CONTEXT
        {string.Join("\n", lines)}
        """;
    }
}
