using JobBoard.AI.Application.Interfaces.AI;

namespace JobBoard.AI.Infrastructure.AI.Services;

public class KeywordToolGroupSelector : IToolGroupSelector
{
    private static readonly Dictionary<string, string[]> GroupKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["company"] = ["company", "companies", "industry", "industries"],
        ["draft"] = ["draft", "drafts", "generate"],
        ["job"] = ["job", "jobs", "publish", "posting", "hire", "hiring", "vacancy", "vacancies"],
        ["system"] = ["mode", "system", "config", "provider", "model", "trace", "debug"]
    };

    public static readonly HashSet<string> AllGroups = [..GroupKeywords.Keys.Append("core")];

    public HashSet<string> SelectGroups(string userMessage, string? conversationSummary)
    {
        var matched = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "core" };

        var text = conversationSummary is not null
            ? $"{userMessage} {conversationSummary}"
            : userMessage;

        foreach (var (group, keywords) in GroupKeywords)
        {
            if (keywords.Any(kw => text.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            {
                matched.Add(group);
            }
        }

        // Draft and job operations always need company tools (for ID resolution)
        if (matched.Contains("draft") || matched.Contains("job"))
            matched.Add("company");

        // Fallback: if only "core" matched, send everything (ambiguous intent)
        return matched.Count == 1 ? AllGroups : matched;
    }
}
